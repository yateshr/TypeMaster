"""Development check: PDFium extraction AND mouse-selection hit areas of exported reports."""
from pathlib import Path
import sys
import pdfplumber
import pypdfium2 as pdfium
from pypdf import PdfReader
root=Path(sys.argv[1])
files=list(root.glob('*.pdf'))
assert len(files)==5, 'Expected all five report samples'
for file in files:
    reader=PdfReader(file,strict=True)
    engine=pdfium.PdfDocument(file)
    hits=total=0
    with pdfplumber.open(file) as parsed:
        for index,page in enumerate(parsed.pages):
            textpage=engine[index].get_textpage()
            text=textpage.get_text_range()
            assert 'Nirmal Poly Plast Private Limited' in text, file
            assert 'Rev ' not in text and 'Build ' not in text, file
            assert reader.pages[index]['/Resources']['/Font'], file
            for word in page.extract_words():
                if not any(c.isalnum() for c in word['text']):
                    continue
                x=(word['x0']+word['x1'])/2
                y=page.height-(word['top']+word['bottom'])/2
                total+=1
                if textpage.get_index(x,y,2,2) is not None:
                    hits+=1
    assert total>20 and hits/total>=.99,(file.name,'selection misses',hits,total)
    if file.name=='typing-test-results.pdf':
        text=engine[0].get_textpage().get_text_range()
        assert 'i [is]' in text and 'hom [home]' in text, text
        assert '2>' in text, text
        assert 'Gross strokes: 133' in text and 'Net strokes: 123' in text, text
        assert '\u200b' not in text, 'Spurious zero-width layout characters'
    print(f'{file.name}: {len(reader.pages)} page(s), {hits}/{total} word hit areas passed')
