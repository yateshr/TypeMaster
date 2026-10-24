Add-Type -AssemblyName PresentationCore,WindowsBase,PresentationFramework
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$assets=Join-Path $root 'Asset\Branding'
$blue=[Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString('#176DEA'))
$gradient=[Windows.Media.LinearGradientBrush]::new([Windows.Media.ColorConverter]::ConvertFromString('#1244AF'),[Windows.Media.ColorConverter]::ConvertFromString('#167BF2'),45)
function Draw-Mark($context) {
    $context.DrawEllipse($gradient,$null,[Windows.Point]::new(32,32),32,32)
    foreach($y in @(17,23)){foreach($x in @(13,19,25,31,37,43,49)){$context.DrawRoundedRectangle([Windows.Media.Brushes]::White,$null,[Windows.Rect]::new($x,$y,4,4),0.8,0.8)}}
    foreach($y in @(29,35,41,47)){foreach($x in @(28,34)){$context.DrawRoundedRectangle([Windows.Media.Brushes]::White,$null,[Windows.Rect]::new($x,$y,4,4),0.8,0.8)}}
}
$face=[Windows.Media.Typeface]::new([Windows.Media.FontFamily]::new('Segoe UI'),[Windows.FontStyles]::Italic,[Windows.FontWeights]::Bold,[Windows.FontStretches]::Normal)
$text=[Windows.Media.FormattedText]::new('TypeMaster',[Globalization.CultureInfo]::InvariantCulture,[Windows.FlowDirection]::LeftToRight,$face,38,$blue,1)
$outline=$text.BuildGeometry([Windows.Point]::new(78,5))
function Render-Logo($file,$wordmark,$pixels) {
    $visual=[Windows.Media.DrawingVisual]::new();$dc=$visual.RenderOpen();Draw-Mark $dc
    if($wordmark){$dc.DrawGeometry($blue,$null,$outline)}
    $dc.Close();$width=if($wordmark){314}else{64};$scale=$pixels/64
    $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new([int]($width*$scale),$pixels,96*$scale,96*$scale,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($visual)
    $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$stream=[IO.File]::Create((Join-Path $assets $file));try{$encoder.Save($stream)}finally{$stream.Dispose()}
}
Render-Logo 'typemaster-icon.png' $false 1024
Render-Logo 'typemaster-wordmark.png' $true 256
$shapes='<defs><linearGradient id="blue" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#1244AF"/><stop offset="1" stop-color="#167BF2"/></linearGradient></defs><circle cx="32" cy="32" r="32" fill="url(#blue)"/>'
foreach($y in @(17,23)){foreach($x in @(13,19,25,31,37,43,49)){$shapes+='<rect x="'+$x+'" y="'+$y+'" width="4" height="4" rx="0.8" fill="white"/>'}}
foreach($y in @(29,35,41,47)){foreach($x in @(28,34)){$shapes+='<rect x="'+$x+'" y="'+$y+'" width="4" height="4" rx="0.8" fill="white"/>'}}
$icon='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" role="img" aria-label="TypeMaster icon">'+$shapes+'</svg>'
$path=$outline.ToString([Globalization.CultureInfo]::InvariantCulture) -replace '^F[01]\s*',''
$wordmark='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 314 64" role="img" aria-label="TypeMaster">'+$shapes+'<path fill="#176DEA" d="'+$path+'"/></svg>'
[IO.File]::WriteAllText((Join-Path $assets 'typemaster-icon.svg'),$icon)
[IO.File]::WriteAllText((Join-Path $assets 'typemaster-wordmark.svg'),$wordmark)
