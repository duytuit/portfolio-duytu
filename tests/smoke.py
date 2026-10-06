"""Functional checks against a running development instance (stdlib only)."""
import http.cookiejar
import json
from pathlib import Path
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid
from html.parser import HTMLParser

BASE = (sys.argv[1] if len(sys.argv) > 1 else 'http://localhost:5000').rstrip('/')
class Page(HTMLParser):
    def __init__(self, html):
        super().__init__(); self.tokens=[]; self.assets=set(); self.feed(html)
    def handle_starttag(self, tag, attrs):
        a=dict(attrs)
        if tag=='input' and a.get('name')=='__RequestVerificationToken': self.tokens.append(a['value'])
        if tag in ('img','script','link'):
            u=a.get('src',a.get('href',''))
            if u.startswith('/'): self.assets.add(u)
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args): return None
jar=http.cookiejar.CookieJar()
client=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar), NoRedirect())
def request(path, data=None):
    req=urllib.request.Request(BASE+path,data=urllib.parse.urlencode(data).encode() if data is not None else None)
    try:
        with client.open(req) as r: return r.status, r.read().decode(errors='replace'), r.headers
    except urllib.error.HTTPError as r: return r.code, r.read().decode(errors='replace'), r.headers
count=0
def check(condition, name):
    global count
    assert condition, name
    count+=1
    print('PASS',name)
paths=['/','/hireus','/app-development','/web-development','/seo-optimization','/campagin-creation','/campaign-creation']
paths += ['/portfolio/'+p for p in ['app1','product1','brand1','book1','app2','book3','product3','book2','app3']]
assets=set()
for path in paths:
    code, html, _=request(path)
    check(code==200 and '<main>' in html, path)
    check(html.count('id="mobile-menu"')==1,'single navigation '+path)
    assets.update(Page(html).assets)
for asset in sorted(assets): check(request(asset)[0]==200,'asset '+asset)
check(request('/portfolio/unknown')[0]==404,'unknown project 404')
check(request('/missing')[0]==404,'unknown route 404')
check(request('/App_Data/contacts.jsonl')[0]==404,'private submission files')
check(request('/contact',{'Name':'No token'})[0]==400,'contact requires CSRF token')
check(request('/newsletter',{'Email':'no-token@example.com'})[0]==400,'newsletter requires CSRF token')
_,html,_=request('/')
token=Page(html).tokens[0]
check(request('/contact',{'__RequestVerificationToken':token,'Email':'invalid'})[0]==400,'contact validation')
check(request('/newsletter',{'__RequestVerificationToken':token,'Email':'invalid'})[0]==400,'newsletter validation')
marker='mvc-smoke-'+uuid.uuid4().hex+'@example.com'
directory=Path(__file__).resolve().parents[1]/'App_Data'
try:
    code,_,headers=request('/contact',{'__RequestVerificationToken':token,'Name':'Smoke test','Email':marker,'Subject':'Conversion check','Message':'Synthetic test data','ReturnPath':'https://example.com'})
    check(code==302 and headers['Location']=='/','contact saved; external redirect rejected')
    code,html,_=request('/')
    check('Your message has been saved' in html,'contact success notice')
    token=Page(html).tokens[0]
    code,_,headers=request('/newsletter',{'__RequestVerificationToken':token,'Email':marker,'ReturnPath':'/hireus'})
    check(code==302 and headers['Location']=='/hireus','newsletter redirect')
    check('Your newsletter registration has been saved' in request('/hireus')[1],'newsletter notice')
    for file in ['contacts.jsonl','subscribers.jsonl']:
        check(any(json.loads(s).get('Email')==marker for s in (directory/file).read_text().splitlines()),'persisted '+file)
finally:
    for file in ['contacts.jsonl','subscribers.jsonl']:
        p=directory/file
        if p.exists():
            lines=p.read_text().splitlines(keepends=True)
            retained=[s for s in lines if json.loads(s).get('Email')!=marker]
            if retained!=lines:
                if retained: p.write_text(''.join(retained))
                else: p.unlink()
    if directory.exists() and not list(directory.iterdir()): directory.rmdir()
print(f'{count} checks passed')
