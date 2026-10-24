from urllib.request import urlopen,Request
from html.parser import HTMLParser
import json,re
url='https://support.microsoft.com/en-us/excel/excel-functions-alphabetical'
html=urlopen(Request(url,headers={'User-Agent':'Mozilla/5.0'}),timeout=40).read().decode('utf8')
class Tables(HTMLParser):
 def __init__(self):super().__init__();self.rows=[];self.row=None;self.cell=None
 def handle_starttag(self,tag,attrs):
  if tag=='tr':self.row=[]
  elif tag in ('td','th') and self.row is not None:self.cell=[]
 def handle_data(self,data):
  if self.cell is not None:self.cell.append(data)
 def handle_endtag(self,tag):
  if tag in ('td','th') and self.cell is not None:self.row.append(' '.join(' '.join(self.cell).split()));self.cell=None
  elif tag=='tr' and self.row is not None:self.rows.append(self.row);self.row=None
p=Tables();p.feed(html)
groups={};versions={}
for row in p.rows:
 if len(row)<2 or ':' not in row[1]:continue
 category=row[1].split(':',1)[0].strip().replace('Look and reference','Lookup and reference')
 label=row[0]; match=re.match(r'^([A-Z][A-Z0-9.]*(?:\s*,\s*[A-Z][A-Z0-9.]*)*)\b',label)
 if not match:continue
 for name in re.split(r'\s*,\s*',match.group(1)):
  groups.setdefault(category,[]).append(name)
  marker=re.search(r'\(([^)]+)\)',label)
  if marker:versions[name]=marker.group(1)
print({k:len(v) for k,v in groups.items()});print('Total:',sum(map(len,groups.values())))
assert sum(map(len,groups.values()))>450
assert all(x in {n for names in groups.values() for n in names} for x in ['SUM','IF','XLOOKUP','FILTER','PMT','TEXTSPLIT','GROUPBY','PIVOTBY'])
result={'source':url,'retrieved':'2026-10-24','groups':[{'name':k,'functions':sorted(set(v))} for k,v in groups.items()],'introduced':versions}
open('Asset/excel-functions.json','w',encoding='utf8').write(json.dumps(result,indent=2,ensure_ascii=False))

