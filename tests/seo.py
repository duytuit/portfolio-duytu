"""Read-only SEO regression checks against a running MVC instance."""
import sys,json,urllib.request,urllib.error,xml.etree.ElementTree as ET
from html.parser import HTMLParser
BASE=sys.argv[1] if len(sys.argv)>1 else 'http://localhost:5085'
class Page(HTMLParser):
 def __init__(self,html):
  super().__init__();self.h1=0;self.canonical=[];self.description=[];self.scripts=[];self.assets=[];self.links=[];self.in_json=False;self.feed(html)
 def handle_starttag(self,tag,attrs):
  a=dict(attrs)
  if tag=='h1':self.h1+=1
  if tag=='link' and a.get('rel')=='canonical':self.canonical.append(a['href'])
  if tag=='meta' and a.get('name')=='description':self.description.append(a['content'])
  if tag=='script':self.in_json=a.get('type')=='application/ld+json'
  if tag in ('img','script','link'):
   src=a.get('src',a.get('href',''))
   if src.startswith('/'):self.assets.append(src)
  if tag=='a' and a.get('href','').startswith('/'):self.links.append(a['href'].split('#')[0])
 def handle_data(self,data):
  if self.in_json:self.scripts.append(json.loads(data))
 def handle_endtag(self,tag):
  if tag=='script':self.in_json=False
class NoRedirect(urllib.request.HTTPRedirectHandler):
 def redirect_request(self,*args):return None
client=urllib.request.build_opener(NoRedirect())
def get(path):
 try:
  with client.open(BASE+path) as r:return r.status,r.read().decode('utf-8',errors='replace'),r.headers
 except urllib.error.HTTPError as r:return r.code,r.read().decode('utf-8',errors='replace'),r.headers
status,xml,_=get('/sitemap.xml');assert status==200
urls=[e.text for e in ET.fromstring(xml).iter('{http://www.sitemaps.org/schemas/sitemap/0.9}loc')]
assert len(urls)==len(set(urls))
assets=set();links=set();descriptions=set()
for url in urls:
 path=url.removeprefix('https://dextech.vn');status,html,_=get(path);assert status==200,(path,status)
 p=Page(html);assert p.h1==1,(path,p.h1);assert p.canonical==[url],path
 assert len(p.description)==1 and p.description[0],path
 assert p.description[0] not in descriptions,('duplicate description',path)
 descriptions.add(p.description[0]);assert p.scripts,path
 types={x['@type'] for x in p.scripts[0]['@graph']};assert {'Person','WebSite'}<=types
 if path.startswith('/blog/'):assert 'BlogPosting' in types
 if path=='/about':assert 'ProfilePage' in types
 assert 'Lorem ipsum' not in html,path
 assets.update(p.assets);links.update(p.links)
 print('PASS SEO',path)
for asset in assets:assert get(asset)[0]==200,asset
for path in links:assert get(path)[0] in (200,301),path
for old,target in {'/hireus':'/contact','/campagin-creation':'/services','/campaign-creation':'/services','/portfolio/app1':'/projects','/web-development':'/services/web-development','/PROJECTS/':'/projects'}.items():
 status,_,headers=get(old);assert status==301 and headers['Location']==target,(old,status)
for path in ['/missing','/portfolio/unknown','/projects/unknown','/services/unknown','/blog/unknown']:
 status,_,headers=get(path);assert status==404 and headers.get('X-Robots-Tag')=='noindex',path
assert 'Sitemap: https://dextech.vn/sitemap.xml' in get('/robots.txt')[1]
status,home,_=get('/');p=Page(home);assert 140<=len(p.description[0])<=160,len(p.description[0])
assert '<title>Duy T' in home and 'Full-stack Developer .NET &amp; ReactJS</title>' in home
assert get('/contact')[0]==200
print('PASS',len(urls),'canonical pages;',len(assets),'assets; redirects; 404; internal links; JSON-LD; homepage description',len(p.description[0]))


# Rejection checks do not send messages or persist synthetic submissions.
for path in ['/contact','/newsletter']:
 try:
  client.open(urllib.request.Request(BASE+path,data=b''))
  raise AssertionError('Missing CSRF accepted')
 except urllib.error.HTTPError as error:
  assert error.code==400,(path,error.code)
print('PASS contact and newsletter reject missing CSRF tokens')
