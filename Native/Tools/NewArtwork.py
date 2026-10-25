"""Rebuild TypeMaster's original SVG/PNG artwork at the legacy canvas sizes.

Authoring dependencies only: Pillow, resvg-py. No dependency is added to the app.
Existing image pixels are never copied: inputs supply filenames and dimensions.
"""
from pathlib import Path
from html import escape
import json, math, re
import xml.etree.ElementTree as ET
from PIL import Image
import resvg_py

ROOT = Path(__file__).resolve().parents[2]
ASSET = ROOT / 'Asset'
OUT = ASSET / 'TypeMasterArtwork'
OUT.mkdir(exist_ok=True)
BLUE, INK, PURPLE = '#176dea', '#172b4d', '#7871d8'
DEFS = '''<defs>
<linearGradient id="key" x2="0" y2="1"><stop stop-color="#f6f5ff"/><stop offset="1" stop-color="#c6c2ef"/></linearGradient>
<linearGradient id="tile" x2=".8" y2="1"><stop stop-color="#eeeaff"/><stop offset=".45" stop-color="#a59ae9"/><stop offset="1" stop-color="#6555cc"/></linearGradient>
<linearGradient id="skin" x2="1" y2=".2"><stop stop-color="#d89975"/><stop offset=".5" stop-color="#f6cfaf"/><stop offset="1" stop-color="#e4ad87"/></linearGradient>
<linearGradient id="panel" x2="0" y2="1"><stop stop-color="#eef4ff"/><stop offset="1" stop-color="#ccd9f6"/></linearGradient>
</defs>'''

def rect(x,y,w,h,fill,stroke=None,r=0,sw=1):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}"'+(f' stroke="{stroke}" stroke-width="{sw}"' if stroke else '')+'/>'

def text(x,y,value,size=12,fill=INK,bold=False,anchor='start',italic=False):
    return f'<text x="{x}" y="{y}" font-family="Segoe UI, Arial, sans-serif" font-size="{size}" fill="{fill}" text-anchor="{anchor}"'+(' font-weight="700"' if bold else '')+(' font-style="italic"' if italic else '')+'>'+escape(str(value))+'</text>'

def line(x,y,xx,yy,color=PURPLE,sw=1):
    return f'<path d="M{x} {y} L{xx} {yy}" stroke="{color}" stroke-width="{sw}" fill="none"/>'

def circle(x,y,r,fill,stroke=None,sw=1):
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}"'+(f' stroke="{stroke}" stroke-width="{sw}"' if stroke else '')+'/>'

def group(body,x=0,y=0,sx=1,sy=None):
    return f'<g transform="translate({x} {y}) scale({sx} {sx if sy is None else sy})">{body}</g>'

def brand(w,h):
    svg=ET.parse(ASSET/'Branding/typemaster-wordmark.svg').getroot()
    body=''.join(ET.tostring(c,encoding='unicode') for c in svg)
    scale=min(w/314,h/64)
    return group(body,0,(h-64*scale)/2,scale)

def hands():
    # Fingertips match the existing live drill's finger cue coordinates.
    s=''
    for right in (False,True):
        if not right:
            tips=[(33,74,62,119),(62,43,77,116),(83,17,91,114),(97,32,105,115)]
            palm='M58 118 Q59 101 79 99 L105 100 Q118 106 119 121 L120 143 L71 143 Z'
            thumb='M109 121 Q121 97 129 95 Q140 95 132 108 L120 132'
        else:
            tips=[(243,20,251,114),(271,16,270,113),(299,28,290,115),(327,50,305,120)]
            palm='M243 116 Q242 101 258 99 L289 100 Q310 106 310 124 L303 143 L252 143 Z'
            thumb='M249 120 Q235 98 225 96 Q215 97 223 109 L244 133'
        s+=f'<path d="{palm}" fill="url(#skin)" stroke="#bc8468" stroke-width="1"/>'
        for tx,ty,bx,by in tips:
            s+=f'<path d="M{bx} {by} Q{(bx+tx)/2} {(by+ty)/2} {tx} {ty+7}" fill="none" stroke="#bb8469" stroke-width="17" stroke-linecap="round"/>'
            s+=f'<path d="M{bx} {by} Q{(bx+tx)/2} {(by+ty)/2} {tx} {ty+7}" fill="none" stroke="url(#skin)" stroke-width="15" stroke-linecap="round"/>'
            s+=rect(tx-4,ty+2,8,10,'#f9ddcb',r=3)
        s+=f'<path d="{thumb}" fill="url(#skin)" stroke="#bc8468" stroke-width="1"/>'
    return s

def keyboard(name,with_hands=True,w=297,h=204):
    layout='azerty' if '_fr' in name or 'azerty' in name else 'qwertz' if '_de' in name else 'nordic' if '_fi' in name or '_se' in name else 'spanish' if '_es' in name else 'qwerty'
    rows=['`1234567890-=','qwertyuiop[]\\',"asdfghjkl;'",'zxcvbnm,./']
    if layout=='azerty': rows=['²1234567890)=','azertyuiop^$*',"qsdfghjklmù",'wxcvbn,;:!']
    if layout=='qwertz':rows=['^1234567890ß´','qwertzuiopü+',"asdfghjklöä",'yxcvbnm,.-']
    if layout=='nordic':rows=['§1234567890+´','qwertyuiopå¨',"asdfghjklöä",'zxcvbnm,.-']
    if layout=='spanish':rows=['º1234567890\'¡','qwertyuiop`+',"asdfghjklñ´",'zxcvbnm,.-']
    active=rows[2][:4]+rows[2][6:10] if 'home_row' in name or 'universal' in name else 'bn' if '_bn' in name else 'ei' if '_ei' in name else 'u' if '_u.' in name else 'f' if 'shift_f' in name else 'a' if 'azerty_a' in name else ''
    s=rect(0,0,297,95,'#dcdcf2','#9c98cd',r=3)
    positions={}
    for row,chars in enumerate(rows):
        shift=[0,11,17,25][row]
        for col,c in enumerate(chars):
            x=3+shift+col*19;y=3+row*18
            if x+18>294:continue
            highlight=c in active
            s+=rect(x,y,18,16,PURPLE if highlight else 'url(#key)','#928cc4',r=1,sw=.7)
            s+=text(x+8,y+11,c.upper(),7,'white' if highlight else '#373362',anchor='middle')
            finger=[0,1,2,3,3,4,4,5,6,7][min(col,9)]
            positions[c]=(x+9,y+16,finger)
    for x,y,ww,label in [(251,3,43,'Back'),(253,39,41,'Enter'),(3,57,21,'Shift'),(250,57,44,'Shift'),(63,76,167,'Space')]:
        hi=('backspace' in name and label=='Back') or ('enter' in name and label=='Enter') or ('shift' in name and label=='Shift' and ('shift_f' not in name or x>100)) or ('space' in name and label=='Space')
        s+=rect(x,y,ww,16,PURPLE if hi else 'url(#key)','#928cc4',r=1,sw=.7)+text(x+ww/2,y+11,label,7,'white' if hi else '#373362',anchor='middle')
    if with_hands:
        s+=group(hands(),0,100,.8,.73)
        for i,c in enumerate(active):
            if c in positions:
                x,y,finger=positions[c];tx,ty=[(33,81),(62,50),(83,24),(97,39),(243,27),(271,23),(299,35),(327,57)][finger]
                s+=f'<g data-key="{escape(c)}" data-finger="{finger+1}">'+line(x,y,tx*.8,100+ty*.73,PURPLE,.85)+'</g>'
    if w==129:return group(s,-164)
    return s

def posture(name,w=200,h=266):
    s=rect(0,0,200,266,'#f1ffdd')
    lean=-8 if 'tip01' in name else 6 if 'tip03' in name else 0
    s+=rect(111,117,83,5,'#adb6b5')+rect(174,122,5,112,'#b5bdbb')
    s+=f'<path d="M161 71 L171 69 L174 112 L156 112 Z" fill="#869494"/>'+line(164,112,164,119,'#869494',4)
    s+=f'<path d="M55 75 Q{50+lean} 105 65 147 L91 155 L105 214 L137 219 L138 228 L91 229 L73 178 L47 169 Q38 160 39 148 L37 93 Z" fill="#213b83"/>'
    s+=f'<path d="M43 76 Q64 70 {66+lean} 87 L77 118 L113 120 L117 125 L72 126 L58 107 L64 146 L40 147 Z" fill="#ffd979"/>'
    s+=circle(54+lean,55,15,'#ebbe92')+f'<path d="M38 58 Q27 31 51 33 Q76 30 70 44 L49 45 L45 58 Z" fill="#60533d"/>'
    s+=f'<path d="M36 83 Q27 102 33 151 Q34 167 57 168 L76 168" fill="none" stroke="#5382d3" stroke-width="9"/>'
    s+=line(56,173,56,231,'#7f8c85',5)+line(28,236,82,236,'#7f8c85',4)
    if 'deskchair' in name:s+=line(18,169,18,229,'#ef8367')+line(80,158,80,231,'#ef8367')+circle(66,113,11,'none','#ef8367',2)
    if 'display' in name:s+=line(69,52,171,69,'#ef8367')+line(69,52,161,112,'#ef8367')
    return group(s,0,0,w/200,h/266)

def gauge(w,h,paused=False,full=True):
    s=rect(0,0,w,h,'#080b10',r=9)
    cx,cy=47,51
    s+=circle(cx,cy,43,'#17212c','#3b434d',5)
    for i in range(30):
        angle=math.radians(140+i*9.3);color='#299ce0' if i<10 else '#e7b04d' if i<21 else '#e96743'
        s+=line(cx+34*math.cos(angle),cy+34*math.sin(angle),cx+40*math.cos(angle),cy+40*math.sin(angle),color,3)
    s+=text(47,89,'WPM',8,'#b7c2d0',anchor='middle')
    if paused:s+=rect(34,31,8,35,'white')+rect(51,31,8,35,'white')
    else:s+=line(47,52,67,26,'#edf4ff',2)+circle(47,52,4,'#edf4ff')
    if w>120:
        s+=text(104,12,'TYPING METER Today',9,'#e8f1ff',True)
        for y,label in [(32,'Words'),(49,'Time'),(68,'WPM')]:s+=text(104,y,label,10,'#bccad8')
    return s

def graph(name,w,h):
    bg='#f2ffdb' if name!='results.gif' and not name.startswith('statistics.gif') else '#e7edf5'
    s=rect(.5,.5,w-1,h-1,bg,'#809763')
    title='Statistics' if name.startswith('statistics') else 'Typing Results' if name.startswith('results') else 'Your Progress'
    if name.startswith('advanced'):title='Typing Courses'
    s+=rect(1,1,w-2,19,'#e4f3bf')+text(8,14,title,10,INK,True)
    if name.startswith('advanced'):
        for i,label in enumerate(['Touch Typing Course','Speed Building Course','Numbers and Specials']):
            yy=27+i*51;s+=rect(6,yy,w-12,21,BLUE,r=2)+text(10,yy+14,label,min(10,w/20),'white',True)
            s+=text(10,yy+36,'Lessons • Practice • Progress',min(8,w/24),'#526245')
        return s
    if title=='Your Progress':
        for i in range(10):s+=rect(18+i*(w-37)/10,h*.55-9-i*h*.024,(w-45)/10,9+i*h*.024,'#f3d46a','#8f945b',sw=.7)
        s+=text(9,h-33,'Time',9)+text(9,h-13,'02:26',18,INK,True)
    else:
        for i,(label,val) in enumerate([('Gross speed','35 wpm'),('Accuracy','97%'),('Net speed','32 wpm')]):
            s+=text(9,35+i*17,label,8)+text(w*.58,35+i*17,val,8,INK,True)
        y=98
        for chart in range(3 if name.startswith('statistics') else 1):
            s+=text(8,y-5,'Typing history',8,INK,True)+line(13,y+h*.25,w-9,y+h*.25,'#8fa363')
            for i in range(8):s+=rect(15+i*(w-37)/8,y+h*.25-(12+(i*17)%36),(w-50)/8,12+(i*17)%36,'#e7ce58','#9aa049',sw=.6)
            y+=82
    return s

def cloud(x,y,scale=1,label=''):
    s=circle(34,25,22,'#fbfdff','#b7bfd6')+circle(60,31,18,'#fbfdff','#b7bfd6')+circle(17,35,14,'#fbfdff','#b7bfd6')+rect(9,28,64,27,'#fbfdff',r=13)
    if label:s+=text(42,46,label,13,'#657198',True,anchor='middle')
    return group(s,x,y,scale)

def game(name,w,h):
    s=rect(0,0,w,h,'#c8cdfe')
    if name=='bubbles':
        for i,(xx,yy,rr) in enumerate([(w*.7,h*.15,15),(w*.24,h*.36,18),(w*.58,h*.55,16),(w*.72,h*.76,20),(w*.28,h*.93,18)]):
            color=['#83bb3c','#7480ee','#8d8cf5','#ec4a94','#6f78e8'][i]
            s+=circle(xx,yy,rr,color)+circle(xx-rr*.25,yy-rr*.3,rr*.25,'#ffffff55')+text(xx,yy+4,'asdfg'[i],min(15,rr),'white',True,anchor='middle')
    elif name=='wordtris':
        s+=rect(8,8,w-16,22,'#85ba21')+text(15,24,'WordTris',14,'white',True)
        for i,label in enumerate(['lake','lion','tiger','bear']):
            y=h-20-i*30;s+=rect(12,y,w-24,26,'url(#panel)','#8385c6',r=2)+text(w/2,y+18,label,16,'#5c6193',anchor='middle')
        s+=rect(15,40,w-30,30,'#edf6ff',r=2)
    elif name in ('clouds','cloud-race'):
        s+=circle(w-29,29,20,'#f5ca3d')
        for i in range(3):s+=cloud(5+(i%2)*w*.35,20+i*h*.27,min(w/120,h/180),['cat','owl','fox'][i] if name=='clouds' else ['+12','+8','+16'][i])
    elif name=='abc-speed-race':
        for i in range(26):
            a=math.radians(i*13-130);xx=w*.5+w*.37*math.cos(a);yy=(h-30)*.5+(h-30)*.36*math.sin(a)
            s+=circle(xx,yy,8,'#76ae3c')+text(xx,yy+4,chr(97+i),11,'white',True,anchor='middle')
    else:
        for row in range(4):
            for col in range(4):
                x=16+col*(w-25)/4;y=12+row*(h-20)/4
                s+=rect(x,y,(w-35)/4,(h-30)/4,'#5c9e3f','#315b39',r=3)
                s+=line(x+8,y+16,x+22,y+16,'#b9de67',6)
                if (row+col)%2:s+=line(x+16,y+16,x+16,y+29,'#b9de67',6)
                s+=text(x+13,y+25,'asdf'[(row+col)%4],10,'#efffd7',True,anchor='middle')
    return s

def tileset():
    s=''
    for row in range(10):
        for col in range(12):
            x=col*32;y=row*32
            s+=rect(x+1,y+1,30,30,'#18372a','#639044',r=3)
            s+=line(x+2,y+16,x+30,y+16,'#8dc950',9)
            if (row+col)%3:s+=line(x+16,y+16,x+16,y+30,'#8dc950',9)
    return s

def laptop(w,h):
    s=rect(19,7,w-38,h*.67,'#b9c5d1','#5d6c82',r=5)+rect(25,13,w-50,h*.57,'#f5f9fd',r=2)
    for i in range(6):s+=rect(35,24+i*9,w-90-(i%3)*10,3,'#d3dfed',r=1)
    s+=f'<path d="M19 {h*.72} L{w-19} {h*.72} L{w-1} {h-5} L1 {h-5} Z" fill="#b7c4d2" stroke="#65788e"/>'
    s+=group(gauge(255,102),w*.49,h*.44,w*.47/255)
    return s

def illustration(name,w,h):
    photo={'typing.jpg':'typing-photo','typing_jr.jpg':'junior-photo','stdtip.bmp':'typing-tip-photo','robinhood.GIF':'robinhood-scene'}.get(name)
    if photo:
        if not (OUT/'Sources'/(photo+'.png')).exists():raise FileNotFoundError('Missing authored photo source '+photo)
        return f'<image href="Sources/{photo}.png" x="0" y="0" width="{w}" height="{h}" preserveAspectRatio="xMidYMid slice"/>'
    if w<=3 or h<=3:return ''
    if name.startswith('crs_icon'):
        glyph={'basic':'abc','empty':'','junior':'abc','numbers':'123','numpad':'▦','specials':'@','speed':'↗','test':'☷'}.get(name[9:-4],'')
        return rect(1,1,68,68,'url(#tile)','#9689d9',r=9)+rect(8,7,54,54,'#ffffff44','#e2deff',r=5)+text(35,46,glyph,26,'#352875',True,anchor='middle')
    if name.startswith('keyboard') or name in ('qwerty.jpg','universal.png'):return keyboard(name,w=w,h=h)
    if name.startswith('ergo') or name.startswith('tip0'):return posture(name,w,h)
    if name.startswith('spmeter'):return gauge(w,h,'paused' in name or name=='spmeter2.bmp')
    if name.startswith(('progress_graph','2progress_graph','results','statistics','advanced_courses')):return graph(name,w,h)
    if name in ('bubbles.gif','wordtris.gif'):return game(name.split('.')[0],w,h)
    if name=='pipe_tileset.bmp':return tileset()
    if name.startswith('logo_'):return brand(w,h)
    if name.startswith('metertip'):return laptop(w,h)
    if name.startswith('menu_test'):
        s=rect(0,0,w,h,'url(#tile)')
        for i,label in enumerate(['Studying','Typing Meter','Custom Review','Typing Test','Games','Statistics','Settings','About']):
            yy=20+i*33;s+=circle(13,yy-4,5,'#eff5ff')+text(24,yy,label,9,'white')
        s+=group(brand(110,25),7,h-31)
        return s
    if name=='checkmark.gif':return rect(0,0,16,16,'#8bcc25',r=2)+'<path d="M3 8l3 3 7-8" fill="none" stroke="white" stroke-width="2"/>'
    if name.startswith('entersymbol'):return text(w/2,h-3,'↵' if name=='entersymbol.gif' else '«',17,'#354448',True,anchor='middle')
    if name=='msgbullet.gif':return '<path d="M6 2l9 5-9 5Z" fill="#3c4744"/>'
    if name=='diffkeys.bmp':
        s=rect(1,1,w-2,h-2,'#f5ffe6','#b9c89c')+text(8,16,'Difficult Words',10,INK,True)
        for i,word in enumerate(['practice','accurate','keyboard','language','comfortable','paragraph','progress','candidate','document','assessment']):
            s+=rect(8,26+i*17,10+(9-i)*4,9,'#e17b85' if i<3 else '#99bc66')+text(57,34+i*17,word,8)
        return s
    if name=='splashtm.bmp':return rect(0,0,w,h,'#f5faff')+group(brand(330,75),80,20)+group(keyboard('keyboard_home_row.gif'),30,110,.76)+group(gauge(255,102),275,138,.7)+text(w/2,h-20,'Typing practice • Candidate assessments',15,INK,True,anchor='middle')
    if name in ('typing.jpg','typing_jr.jpg','stdtip.bmp'):
        return rect(0,0,w,h,'#edf5ff')+group(posture('ergo.gif'),0,0,w/200,h/266)
    if name=='robinhood.GIF':
        s=rect(0,0,w,h,'#e5ecd9')
        for i in range(14):
            x=i*67;s+=rect(x+20,80+(i%3)*20,9,h,'#726a4c')+f'<path d="M{x-15} 190 L{x+24} 13 L{x+70} 190 Z" fill="#4d7758"/>'
        return s+text(w/2,h*.63,'TypeMaster',64,'#fafded',True,anchor='middle',italic=True)
    raise ValueError('Unclassified legacy artwork '+name)

manifest={'version':1,'author':'TypeMaster project','fingertips':[[129,100],[33,81],[62,50],[83,24],[97,39],[243,27],[271,23],[299,35],[327,57],[225,101]],'legacy':{},'extras':{}}

def save(name,w,h,body,category,legacy=False):
    stem=name.replace('/','-');filename=stem+'.png'
    svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}" role="img" aria-label="{escape(name)}">{DEFS}{body}</svg>'
    (OUT/(stem+'.svg')).write_text(svg,encoding='utf-8')
    (OUT/filename).write_bytes(resvg_py.svg_to_bytes(svg_string=svg,resources_dir=str(OUT)))
    manifest['legacy' if legacy else 'extras'][name]={'file':filename,'width':w,'height':h,'category':category}

for p in sorted(ASSET.iterdir()):
    if p.suffix.lower() not in ('.png','.jpg','.gif','.bmp'):continue
    with Image.open(p) as im:w,h=im.size
    save(p.name,w,h,illustration(p.name,w,h),'legacy-sized original redraw',True)

save('hands',367,143,hands(),'finger cue aligned illustration')
save('finger-patch',22,22,group(hands(),-86,-21),'finger cue background')
save('meter-launch',236,133,laptop(236,133),'meter introduction')
save('close',17,17,rect(0,0,17,17,'#655cff')+'<path d="M4 4l9 9M13 4l-9 9" stroke="white" stroke-width="2"/>','control')
save('profile',22,21,circle(11,6,5,'#536a84')+'<path d="M2 21v-3q0-7 9-7t9 7v3Z" fill="#536a84"/>','control')
save('training-profile',22,21,rect(2,2,18,17,'#f7faff','#536a84',2)+line(6,7,16,7,'#536a84',2)+line(6,11,16,11,'#536a84',2)+line(6,15,13,15,'#536a84',2),'control')
for name,w,h in [('bubbles',200,182),('wordtris',168,171),('clouds',190,179),('abc-speed-race',170,173),('pipe-game',168,173),('cloud-race',190,173)]:save('game-'+name,w,h,game(name,w,h),'game selector')
for name,w,label in [('bubbles',110,'Bubbles'),('wordtris',110,'WordTris'),('clouds',110,'Clouds'),('abc-speed-race',191,'ABC Speed Race'),('pipe-game',170,'Pipe Game'),('cloud-race',193,'Cloud Race')]:
    h=31 if w==110 else 32
    save('caption-'+name,w,h,rect(0,0,w,h,'#c8cdfe')+text(w/2,h-7,label,min(24,w/(len(label)*.57)),'#29245c',True,'middle',True),'game caption')
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(f'Created {len(manifest["legacy"])} legacy-sized redraws and {len(manifest["extras"])} standalone UI assets in {OUT}')
