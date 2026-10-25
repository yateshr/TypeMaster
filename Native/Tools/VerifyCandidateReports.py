"""Validate the two requested candidate samples, including viewer text selection."""
from pathlib import Path
import sys
from datetime import date
import pdfplumber
import pypdfium2 as pdfium

folder=Path(sys.argv[1])
cases=[("sample-candidate-short-skills.pdf","Sample Candidate",6),("sample-candidate-long-skills.pdf","Sample Candidate With A Longer Name",112)]
for filename,name,skills in cases:
    file=folder/filename
    engine=pdfium.PdfDocument(file)
    with pdfplumber.open(file) as report:
        pages=report.pages
        texts=[page.extract_text() or "" for page in pages]
        assert texts[0].count("Attempt ")==4,(filename,"four typing attempts on page one")
        assert all("Attempt " not in text and "Typing Tests" not in text for text in texts[1:])
        assert sum(text.count("✓") for text in texts)==skills,(filename,"all skills retained")
        assert "Remarks" in texts[-1] and all("Remarks" not in text for text in texts[:-1])
        assert date.today().strftime("%d-%m-%Y")+" 09:45 AM" in texts[0]
        assert "CANDIDATE ASSESSMENT" not in texts[0]
        assert len(pages)==(1 if skills==6 else 2),(filename,"three-column page count")
        hits=total=0
        for index,(page,text) in enumerate(zip(pages,texts)):
            assert name in text
            assert "Test taken by: Sample Interviewer" in text
            assert f"Page {index+1} of {len(pages)}" in text
            assert "Nirmal Poly Plast Private Limited" in text
            assert "PROGRESS REPORT" in text
            if index:
                assert "Technical Skills | Excel - continued" in text
                assert "Excel (110) - continued" in text
            assert any(abs(line['top']-(81 if index==0 else 71))<.15 and abs(line['x1']-line['x0'])>530 for line in page.lines),"Approved masthead divider missing"
            viewer=engine[index].get_textpage()
            for word in page.extract_words():
                assert word["x0"]>=25 and word["x1"]<=571,(filename,"horizontal clipping",word["text"])
                assert word["bottom"]<780 or word["top"]>785,(filename,"footer overlap",word["text"])
                if any(c.isalnum() for c in word["text"]):
                    total+=1
                    x=(word["x0"]+word["x1"])/2
                    y=page.height-(word["top"]+word["bottom"])/2
                    hits+=viewer.get_index(x,y,2,2) is not None
        assert hits/total>=.99,(filename,"text selection",hits,total)
        print(f"{filename}: {len(pages)} pages, 4 attempts, {skills} skills, {hits}/{total} selectable word areas; ordering and footer checks passed")
