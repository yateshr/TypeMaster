using System.Globalization;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace TypeMaster.Native;

// Exports the actual WPF glyphs and drawing geometry; only image assets are raster images.
public static class NativePdf
{
    public static void Export(FlowDocument document, string path) => Export(ReportFactory.Paginator(document), path);
    public static void Export(DocumentPaginator paginator, string path)
    {
        paginator.ComputePageCount();
        if (paginator.PageCount == 0) throw new InvalidOperationException("The report has no pages.");
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { new VectorDocument().Write(paginator, temporary); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    static string N(double value) => value.ToString("0.#####", CultureInfo.InvariantCulture);
    static string Hex(string value) => Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(value));
    sealed class VectorDocument
    {
        readonly List<byte[]> objects = [[]];
        readonly Dictionary<string, EmbeddedFont> fonts = [];
        readonly Dictionary<ImageSource, (string Name, int Id)> images = [];
        readonly Dictionary<string, (string Name, int Id)> alphas = [];
        readonly List<int> pages = [];
        StringBuilder content = new();
        int Reserve() { objects.Add([]); return objects.Count - 1; }
        int Object(string text) { int id = Reserve(); Set(id, text); return id; }
        void Set(int id, string text) => objects[id] = Encoding.ASCII.GetBytes(text);
        int Stream(byte[] data, string extra = "")
        {
            using var buffer = new MemoryStream();
            using (var zip = new ZLibStream(buffer, CompressionLevel.Optimal, true)) zip.Write(data);
            byte[] compressed = buffer.ToArray(); int id = Reserve(); using var body = new MemoryStream();
            body.Write(Encoding.ASCII.GetBytes($"<< {extra} /Length {compressed.Length} /Filter /FlateDecode >>\nstream\n"));
            body.Write(compressed); body.Write(Encoding.ASCII.GetBytes("\nendstream")); objects[id] = body.ToArray(); return id;
        }
        public void Write(DocumentPaginator paginator, string path)
        {
            int catalog = Reserve(), pageTree = Reserve();
            for (int i = 0; i < paginator.PageCount; i++)
            {
                var page = paginator.GetPage(i); content = new(); content.Append($"q .75 0 0 -.75 0 {N(page.Size.Height * .75)} cm\n");
                Visual(page.Visual, 1); content.Append("Q\n"); int commands = Stream(Encoding.ASCII.GetBytes(content.ToString()));
                string resources = "/Font << " + string.Join(" ", fonts.Values.Select(f => $"/{f.Name} {f.Id} 0 R")) +
                    " >> /XObject << " + string.Join(" ", images.Values.Select(image => $"/{image.Name} {image.Id} 0 R")) +
                    " >> /ExtGState << " + string.Join(" ", alphas.Values.Select(a => $"/{a.Name} {a.Id} 0 R")) + " >>";
                pages.Add(Object($"<< /Type /Page /Parent {pageTree} 0 R /MediaBox [0 0 {N(page.Size.Width * .75)} {N(page.Size.Height * .75)}] /Resources << {resources} >> /Contents {commands} 0 R >>"));
            }
            foreach (var font in fonts.Values) font.Complete(this);
            Set(catalog, $"<< /Type /Catalog /Pages {pageTree} 0 R >>");
            Set(pageTree, $"<< /Type /Pages /Count {pages.Count} /Kids [{string.Join(" ", pages.Select(p => $"{p} 0 R"))}] >>");
            int info = Object($"<< /Producer (TypeMaster vector PDF) /Author <FEFF{Hex(ReportBranding.Company)}> >>");
            using var output = File.Create(path);
            void Out(string text) => output.Write(Encoding.ASCII.GetBytes(text));
            Out("%PDF-1.7\n% TypeMaster selectable vector report\n"); var offsets = new List<long> { 0 };
            for (int id = 1; id < objects.Count; id++) { offsets.Add(output.Position); Out(id + " 0 obj\n"); output.Write(objects[id]); Out("\nendobj\n"); }
            long xref = output.Position; Out($"xref\n0 {objects.Count}\n0000000000 65535 f \n");
            foreach (long offset in offsets.Skip(1)) Out(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
            Out($"trailer\n<< /Size {objects.Count} /Root {catalog} 0 R /Info {info} 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        }
        void Matrix(Matrix m) { if (!m.IsIdentity) content.Append($"{N(m.M11)} {N(m.M12)} {N(m.M21)} {N(m.M22)} {N(m.OffsetX)} {N(m.OffsetY)} cm\n"); }
        void Alpha(double opacity)
        {
            string key = N(Math.Clamp(opacity, 0, 1));
            if (!alphas.TryGetValue(key, out var alpha)) { alpha = ("GS" + (alphas.Count + 1), Object($"<< /Type /ExtGState /ca {key} /CA {key} >>")); alphas[key] = alpha; }
            content.Append('/').Append(alpha.Name).Append(" gs\n");
        }
        void Color(Brush brush, bool stroke, double opacity)
        {
            if (brush is not SolidColorBrush solid) throw new NotSupportedException("The report contains an unsupported non-solid drawing brush.");
            var c = solid.Color; Alpha(opacity * brush.Opacity * c.A / 255d);
            content.Append($"{N(c.R / 255d)} {N(c.G / 255d)} {N(c.B / 255d)} {(stroke ? "RG" : "rg")}\n");
        }
        void Visual(Visual visual, double opacity)
        {
            content.Append("q\n"); var offset = VisualTreeHelper.GetOffset(visual); Matrix(new Matrix(1, 0, 0, 1, offset.X, offset.Y));
            if (VisualTreeHelper.GetTransform(visual) is { } transform) Matrix(transform.Value);
            opacity *= VisualTreeHelper.GetOpacity(visual); if (VisualTreeHelper.GetClip(visual) is { } clip) Clip(clip);
            if (VisualTreeHelper.GetDrawing(visual) is { } drawing) Drawing(drawing, opacity);
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++) if (VisualTreeHelper.GetChild(visual, i) is Visual child) Visual(child, opacity);
            content.Append("Q\n");
        }
        void Drawing(Drawing drawing, double opacity)
        {
            if (opacity <= 0) return;
            if (drawing is DrawingGroup group)
            {
                content.Append("q\n"); if (group.Transform != null) Matrix(group.Transform.Value); if (group.ClipGeometry != null) Clip(group.ClipGeometry);
                foreach (var child in group.Children) Drawing(child, opacity * group.Opacity); content.Append("Q\n");
            }
            else if (drawing is GlyphRunDrawing glyph) Glyph(glyph, opacity);
            else if (drawing is ImageDrawing image) Image(image.ImageSource, image.Rect, opacity);
            else if (drawing is GeometryDrawing geometry)
            {
                if (geometry.Brush is VisualBrush visualBrush && visualBrush.Visual is { } source)
                {
                    content.Append("q\n"); Clip(geometry.Geometry);
                    Rect box = visualBrush.ViewboxUnits == BrushMappingMode.Absolute ? visualBrush.Viewbox : VisualTreeHelper.GetDescendantBounds(source);
                    Rect bounds = geometry.Geometry.Bounds;
                    if (box.Width > 0 && box.Height > 0) { double sx = bounds.Width / box.Width, sy = bounds.Height / box.Height; Matrix(new Matrix(sx, 0, 0, sy, bounds.X - box.X * sx, bounds.Y - box.Y * sy)); Visual(source, opacity * visualBrush.Opacity); }
                    content.Append("Q\n");
                }
                else
                {
                    content.Append("q\n"); if (geometry.Brush != null) Color(geometry.Brush, false, opacity);
                    if (geometry.Pen is { } pen) { Color(pen.Brush, true, opacity); content.Append(N(pen.Thickness) + " w\n"); }
                    bool even = Path(geometry.Geometry);
                    content.Append(geometry.Brush != null ? (geometry.Pen != null ? (even ? "B*\n" : "B\n") : (even ? "f*\n" : "f\n")) : geometry.Pen != null ? "S\n" : "n\n"); content.Append("Q\n");
                }
            }
            else throw new NotSupportedException("The report contains an unsupported drawing type: " + drawing.GetType().Name);
        }
        bool Path(Geometry geometry)
        {
            var flat = geometry.GetFlattenedPathGeometry(.01, ToleranceType.Absolute); var transform = flat.Transform;
            Point P(Point p) => transform == null ? p : transform.Transform(p);
            foreach (var figure in flat.Figures)
            {
                var start = P(figure.StartPoint); content.Append($"{N(start.X)} {N(start.Y)} m\n");
                foreach (var segment in figure.Segments)
                {
                    IEnumerable<Point> points = segment is LineSegment line ? [line.Point] : segment is PolyLineSegment poly ? poly.Points : throw new NotSupportedException("Unexpected unflattened path.");
                    foreach (var point in points) { var p = P(point); content.Append($"{N(p.X)} {N(p.Y)} l\n"); }
                }
                if (figure.IsClosed) content.Append("h\n");
            }
            return flat.FillRule == FillRule.EvenOdd;
        }
        void Clip(Geometry geometry) { bool even = Path(geometry); content.Append(even ? "W* n\n" : "W n\n"); }
        void Image(ImageSource source, Rect rect, double opacity)
        {
            if (!images.TryGetValue(source, out var image))
            {
                if (source is not BitmapSource bitmap) throw new NotSupportedException("Only bitmap image assets can be embedded in a report.");
                var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                int width = converted.PixelWidth, height = converted.PixelHeight; var pixels = new byte[width * height * 4]; converted.CopyPixels(pixels, width * 4, 0);
                var rgb = new byte[width * height * 3]; var alpha = new byte[width * height]; bool transparent = false;
                for (int i = 0, j = 0, k = 0; i < pixels.Length; i += 4) { rgb[j++] = pixels[i + 2]; rgb[j++] = pixels[i + 1]; rgb[j++] = pixels[i]; alpha[k++] = pixels[i + 3]; transparent |= pixels[i + 3] != 255; }
                string mask = transparent ? $" /SMask {Stream(alpha, $"/Type /XObject /Subtype /Image /Width {width} /Height {height} /ColorSpace /DeviceGray /BitsPerComponent 8")} 0 R" : "";
                int id = Stream(rgb, $"/Type /XObject /Subtype /Image /Width {width} /Height {height} /ColorSpace /DeviceRGB /BitsPerComponent 8{mask}");
                image = ("I" + (images.Count + 1), id); images[source] = image;
            }
            content.Append("q\n"); Alpha(opacity); content.Append($"{N(rect.Width)} 0 0 {N(-rect.Height)} {N(rect.X)} {N(rect.Bottom)} cm /{image.Name} Do\nQ\n");
        }
        void Glyph(GlyphRunDrawing drawing, double opacity)
        {
            var run = drawing.GlyphRun; if (run.GlyphIndices.Count == 0) return;
            string key = run.GlyphTypeface.FontUri.AbsoluteUri + "|" + string.Join("|", run.GlyphTypeface.FamilyNames.Values) + "|" + string.Join("|", run.GlyphTypeface.FaceNames.Values);
            if (!fonts.TryGetValue(key, out var font)) { font = new EmbeddedFont("F" + (fonts.Count + 1), Reserve(), run.GlyphTypeface); fonts[key] = font; }
            string[] unicode = new string[run.GlyphIndices.Count]; string actual = run.Characters == null ? "" : new string(run.Characters.ToArray());
            if (actual.Length > 0 && run.ClusterMap?.Count == actual.Length)
            {
                foreach (var cluster in Enumerable.Range(0, actual.Length).GroupBy(i => run.ClusterMap[i])) if (cluster.Key < unicode.Length) unicode[cluster.Key] = string.Concat(cluster.Select(i => actual[i]));
            }
            else if (actual.Length == unicode.Length) for (int i = 0; i < unicode.Length; i++) unicode[i] = actual[i].ToString();
            for (int i = 0; i < unicode.Length; i++) unicode[i] ??= actual.Length > 0 ? "\u200B" : font.Unicode(run.GlyphIndices[i]);
            if (actual.Length == 0) actual = string.Concat(unicode);
            if(actual.All(c => c == '\u200B' || c == '\uFEFF')) return;
            bool outlined = run.IsSideways || (run.BidiLevel & 1) != 0 || unicode.Any(value => value == "\u200B" || value.Length != 1);
            content.Append("q\n"); Color(drawing.ForegroundBrush, false, opacity);
            if (outlined) { Path(run.BuildGeometry()); content.Append("f\n"); }
            if(outlined) content.Append($"/Span << /ActualText <FEFF{Hex(actual)}> >> BDC\n");
            content.Append($"BT /{font.Name} {N(run.FontRenderingEmSize)} Tf {(outlined ? 3 : 0)} Tr\n");
            if (outlined)
            {
                // Preserve shaped outlines, and expose the complete logical run as one
                // selectable span. Splitting a ligature into glyph text corrupts copying.
                double advance=run.AdvanceWidths.Sum();
                ushort cid=font.Register(run.GlyphIndices[0],actual,advance/run.FontRenderingEmSize*1000);
                double left=run.BaselineOrigin.X-((run.BidiLevel&1)!=0?advance:0);
                content.Append($"1 0 0 -1 {N(left)} {N(run.BaselineOrigin.Y)} Tm <{cid:X4}> Tj\nET\nEMC\nQ\n");
                return;
            }
            double cursor = 0;
            for (int i = 0; i < run.GlyphIndices.Count; i++)
            {
                ushort cid = font.Register(run.GlyphIndices[i], unicode[i]); var offset = run.GlyphOffsets?.Count > i ? run.GlyphOffsets[i] : new Point();
                double x = run.BaselineOrigin.X + ((run.BidiLevel & 1) != 0 ? -cursor - run.AdvanceWidths[i] : cursor) + offset.X, y = run.BaselineOrigin.Y - offset.Y;
                content.Append($"1 0 0 -1 {N(x)} {N(y)} Tm <{cid:X4}> Tj\n"); cursor += run.AdvanceWidths[i];
            }
            content.Append("ET\nQ\n");
        }
        sealed class EmbeddedFont(string name, int id, GlyphTypeface face)
        {
            public string Name => name;
            public int Id => id;
            readonly Dictionary<(ushort Glyph, string Unicode, double Width), ushort> cids = [];
            Dictionary<ushort, string>? reverse;
            public string Unicode(ushort glyph) { reverse ??= face.CharacterToGlyphMap.GroupBy(p => p.Value).ToDictionary(g => g.Key, g => char.ConvertFromUtf32(g.First().Key)); return reverse.GetValueOrDefault(glyph, "\uFFFD"); }
            public ushort Register(ushort glyph, string unicode, double? width=null) { var key = (glyph, unicode, width??face.AdvanceWidths[glyph]*1000); if (!cids.TryGetValue(key, out ushort cid)) { if (cids.Count >= 65534) throw new InvalidOperationException("Too many distinct characters in one report font."); cid = (ushort)(cids.Count + 1); cids[key] = cid; } return cid; }
            public void Complete(VectorDocument pdf)
            {
                using var source = face.GetFontStream(); using var buffer = new MemoryStream(); source.CopyTo(buffer); byte[] program = buffer.ToArray();
                if (program.Length >= 4 && Encoding.ASCII.GetString(program, 0, 4) == "ttcf") program = ExtractCollectionFace(program);
                if (program.Length < 4 || Encoding.ASCII.GetString(program, 0, 4) == "OTTO") throw new NotSupportedException("The report font cannot be embedded as TrueType: " + face.FontUri);
                int file = pdf.Stream(program, "/Length1 " + program.Length); string baseName = "TypeMaster" + name;
                int descriptor = pdf.Object($"<< /Type /FontDescriptor /FontName /{baseName} /Flags 32 /FontBBox [-1000 -1000 3000 2000] /ItalicAngle {(face.Style == FontStyles.Italic ? "-12" : "0")} /Ascent {N(face.Baseline * 1000)} /Descent {N(-(face.Height - face.Baseline) * 1000)} /CapHeight {N(face.CapsHeight * 1000)} /StemV 80 /FontFile2 {file} 0 R >>");
                var map = new byte[(cids.Count + 1) * 2]; var widths = new StringBuilder();
                foreach (var item in cids.OrderBy(p => p.Value)) { map[item.Value * 2] = (byte)(item.Key.Glyph >> 8); map[item.Value * 2 + 1] = (byte)item.Key.Glyph; widths.Append(item.Value).Append(" [").Append(N(item.Key.Width)).Append("] "); }
                int mapping = pdf.Stream(map);
                int descendant = pdf.Object($"<< /Type /Font /Subtype /CIDFontType2 /BaseFont /{baseName} /CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> /FontDescriptor {descriptor} 0 R /DW 1000 /W [{widths}] /CIDToGIDMap {mapping} 0 R >>");
                var cmap = new StringBuilder("/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n/CMapName /TypeMasterUnicode def\n/CMapType 2 def\n1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n");
                foreach (var batch in cids.OrderBy(p => p.Value).Chunk(100)) { cmap.Append(batch.Length).Append(" beginbfchar\n"); foreach (var item in batch) cmap.Append('<').Append(item.Value.ToString("X4")).Append("> <").Append(Hex(item.Key.Unicode)).Append(">\n"); cmap.Append("endbfchar\n"); }
                cmap.Append("endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend\n");
                int unicode = pdf.Stream(Encoding.ASCII.GetBytes(cmap.ToString()));
                pdf.Set(id, $"<< /Type /Font /Subtype /Type0 /BaseFont /{baseName} /Encoding /Identity-H /DescendantFonts [{descendant} 0 R] /ToUnicode {unicode} 0 R >>");
            }
            // Windows multilingual fallbacks often live in TTC collections. Embed only
            // the selected face, rewriting collection-relative table offsets/checksums.
            byte[] ExtractCollectionFace(byte[] collection)
            {
                ushort U16(int at)=>BinaryPrimitives.ReadUInt16BigEndian(collection.AsSpan(at,2));
                int U32(int at)=>checked((int)BinaryPrimitives.ReadUInt32BigEndian(collection.AsSpan(at,4)));
                List<(string Tag,int Offset,int Length)> Tables(int start)=>Enumerable.Range(0,U16(start+4)).Select(i=>start+12+i*16).Select(at=>(Encoding.ASCII.GetString(collection,at,4),U32(at+8),U32(at+12))).ToList();
                int selected=-1,best=int.MinValue;
                for(int index=0;index<U32(8);index++)
                {
                    int start=U32(12+index*4);var tables=Tables(start);var maxp=tables.First(t=>t.Tag=="maxp");
                    if(U16(maxp.Offset+4)!=face.GlyphCount)continue;
                    int score=0;var name=tables.First(t=>t.Tag=="name");int strings=name.Offset+U16(name.Offset+4);
                    for(int n=0;n<U16(name.Offset+2);n++)
                    {
                        int at=name.Offset+6+n*12;int platform=U16(at),nameId=U16(at+6);if(platform!=0&&platform!=3)continue;
                        string value=Encoding.BigEndianUnicode.GetString(collection,strings+U16(at+10),U16(at+8));
                        if((nameId==1||nameId==16)&&face.FamilyNames.Values.Contains(value))score+=4;
                        if((nameId==2||nameId==17)&&face.FaceNames.Values.Contains(value))score+=8;
                    }
                    var os2=tables.FirstOrDefault(t=>t.Tag=="OS/2");if(os2.Length>=8&&U16(os2.Offset+4)==face.Weight.ToOpenTypeWeight())score+=20;
                    var head=tables.First(t=>t.Tag=="head");double units=U16(head.Offset+18);var hhea=tables.First(t=>t.Tag=="hhea");int metrics=U16(hhea.Offset+34);var hmtx=tables.First(t=>t.Tag=="hmtx");
                    foreach(var glyph in face.AdvanceWidths.Take(200))if(Math.Abs(U16(hmtx.Offset+Math.Min(glyph.Key,metrics-1)*4)/units-glyph.Value)<.00001)score++;
                    if(score>best){best=score;selected=start;}
                }
                if(selected<0)throw new NotSupportedException("The selected collection font could not be located: "+face.FontUri);
                var chosen=Tables(selected).Where(t=>t.Tag!="DSIG").ToArray();int count=chosen.Length,total=12+16*count;
                foreach(var table in chosen)total+=(table.Length+3)&~3;
                var result=new byte[total];Array.Copy(collection,selected,result,0,12);
                void W16(int at,int value)=>BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(at,2),(ushort)value);
                void W32(int at,uint value)=>BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(at,4),value);
                uint Sum(int offset,int length){uint sum=0;for(int at=offset;at<offset+length;at+=4)sum=unchecked(sum+BinaryPrimitives.ReadUInt32BigEndian(result.AsSpan(at,4)));return sum;}
                int power=1,selector=0;while(power*2<=count){power*=2;selector++;}W16(4,count);W16(6,power*16);W16(8,selector);W16(10,count*16-power*16);
                int position=12+16*count,headPosition=0;
                for(int i=0;i<count;i++)
                {
                    var table=chosen[i];int directory=12+i*16;Encoding.ASCII.GetBytes(table.Tag).CopyTo(result,directory);Array.Copy(collection,table.Offset,result,position,table.Length);
                    if(table.Tag=="head"){headPosition=position;W32(position+8,0);}
                    W32(directory+4,Sum(position,(table.Length+3)&~3));W32(directory+8,(uint)position);W32(directory+12,(uint)table.Length);position+=(table.Length+3)&~3;
                }
                W32(headPosition+8,unchecked(0xB1B0AFBA-Sum(0,result.Length)));return result;
            }
        }
    }
}




