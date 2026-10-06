const assert=require('node:assert/strict');
const {chromium}=require('playwright');
const base=process.env.BASE_URL || 'http://127.0.0.1:5000';
(async()=>{
 const b=await chromium.launch({executablePath:process.env.CHROMIUM_EXECUTABLE,args:['--no-sandbox']});
 try {
  for(const width of [1440,1024,768,390]) {
   const p=await b.newPage({viewport:{width,height:900},reducedMotion:'reduce'});
   const errors=[];p.on('pageerror',e=>errors.push(e.message));
   await p.goto(base,{waitUntil:'load'});
   // Load all lazy photos before measuring the page and taking screenshots.
   await p.locator('[data-editorial-photo]').evaluateAll(images=>images.forEach(e=>e.loading='eager'));
   await p.waitForFunction(()=>[...document.querySelectorAll('[data-editorial-photo]')].every(e=>e.complete&&e.naturalWidth>0));
   const metrics=await p.evaluate(()=>({height:document.documentElement.scrollHeight,overflow:document.documentElement.scrollWidth>innerWidth,portrait:document.querySelector('.hero-portrait img').getBoundingClientRect().width}));
   assert(!metrics.overflow,`overflow at ${width}`);
   if(width===1440){assert(metrics.portrait>650, 'portrait must be wider');assert(metrics.height<13365*.75,'reduce desktop scrolling by at least 25%');}
   if(width===390){assert(metrics.portrait>318,'wider mobile portrait');assert(metrics.height<16640*.8,'reduce mobile scrolling by at least 20%');}
   assert.deepEqual(errors,[]);
   console.log('PASS compact layout',width,metrics);
   if(width===1440 || width===390)await p.screenshot({path:`/tmp/portfolio-compact-${width}.png`,fullPage:true});
   await p.close();
  }
 }finally{await b.close();}
})().catch(e=>{console.error(e);process.exit(1)});
