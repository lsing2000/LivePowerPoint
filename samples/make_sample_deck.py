"""Regenerates LiveTableDemo.pptx (python-pptx, MIT). Run: pip install python-pptx && python make_sample_deck.py"""
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
p=Presentation(); p.slide_width=Inches(13.333); p.slide_height=Inches(7.5)
def slide(title, hdr, rows, name):
    s=p.slides.add_slide(p.slide_layouts[5]); s.shapes.title.text=title
    gf=s.shapes.add_table(len(rows)+1,len(hdr),Inches(1.5),Inches(2),Inches(10),Inches(3.5)); gf.name=name
    t=gf.table
    for c,h in enumerate(hdr):
        cell=t.cell(0,c); cell.text=h; cell.fill.solid(); cell.fill.fore_color.rgb=RGBColor(0x1F,0x3A,0x5F)
        r=cell.text_frame.paragraphs[0].runs[0]; r.font.size=Pt(28); r.font.bold=True; r.font.color.rgb=RGBColor(255,255,255)
    for i,row in enumerate(rows,1):
        for c,v in enumerate(row):
            cell=t.cell(i,c); cell.text=str(v); cell.text_frame.paragraphs[0].runs[0].font.size=Pt(26)
slide("Sales Summary",["Department","Sales","Status"],[["North",125000,"On target"],["South",98500,"Below target"],["West",143200,"Above target"]],"SalesTable")
slide("Inventory",["Item","Stock","Health"],[["Widgets",420,"82.0%"],["Gadgets",135,"41.0%"],["Gizmos",980,"95.0%"]],"InventoryTable")
p.save("LiveTableDemo.pptx")
