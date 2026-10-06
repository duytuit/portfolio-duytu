const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const base = process.env.BASE_URL || 'http://127.0.0.1:5000';
(async () => {
 const browser = await chromium.launch({executablePath:process.env.CHROMIUM_EXECUTABLE, headless:true,args:['--no-sandbox']});
 try {
  const page = await browser.newPage({viewport:{width:1440,height:1000},reducedMotion:'no-preference'});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));

  await page.goto(base,{waitUntil:'load'});
  assert.equal(await page.locator('.hero-copy h1').evaluate(e=>getComputedStyle(e).fontSize),'72px');
  assert.equal(await page.locator('.hero-description').first().evaluate(e=>getComputedStyle(e).fontSize),'16px');
  assert.equal(await page.locator('[data-editorial-photo]').count(),5);
  assert.equal(await page.locator('.hero-copy h1 .text-word').count(),4);
  await page.waitForFunction(()=>!document.querySelector('.hero-copy h1').classList.contains('is-revealing'));
  const text=await page.locator('.hero-copy h1').textContent();
  const breaks=await page.locator('.hero-copy h1 br').count();
  assert(await page.locator('.hero-copy h1 > .text-word').first().evaluate(e=>getComputedStyle(e).color!=='rgb(234, 88, 12)'));
  await page.locator('#portfolio').evaluate(e=>e.scrollIntoView({behavior:'instant'}));
  await page.waitForFunction(()=>[...document.querySelectorAll('.project-photo')].every(e=>e.complete && e.naturalWidth>0));
  assert.equal(await page.locator('.project-visual.photo-loaded').count(),4);
  assert(await page.locator('[data-editorial-photo]').evaluateAll(images=>images.every(e=>new URL(e.src).origin===location.origin)));
  await page.emulateMedia({reducedMotion:'reduce'});
  await page.waitForFunction(()=>document.querySelectorAll('.text-word').length===0);
  assert.equal(await page.locator('.hero-copy h1').textContent(),text);
  assert.equal(await page.locator('.hero-copy h1 br').count(),breaks);
  await page.emulateMedia({reducedMotion:'no-preference'});
  await page.waitForFunction(()=>document.querySelectorAll('.text-word').length>0);
  assert.equal(await page.locator('.hero-copy h1').textContent(),text);
  await page.setViewportSize({width:390,height:844});
  await page.goto(base,{waitUntil:'load'});
  assert.equal(await page.locator('.hero-copy h1').evaluate(e=>getComputedStyle(e).fontSize),'35.2px');
  assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
  assert.deepEqual(errors,[]);
  console.log('PASS original desktop/mobile typography; word animation preserves text and line breaks; live reduced motion; local photographs; no browser errors');
 } finally { await browser.close(); }
})().catch(e=>{console.error(e);process.exit(1)});
