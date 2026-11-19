import { createHash } from 'node:crypto';
import { spawn, execFileSync } from 'node:child_process';
import { readFile, writeFile, mkdir, mkdtemp, appendFile, readdir } from 'node:fs/promises';
import { resolve, relative, dirname, isAbsolute, sep } from 'node:path';
import { pathToFileURL, fileURLToPath } from 'node:url';
import os from 'node:os';

export const FRACTIONS = [0, 0.25, 0.5, 0.75, 1];
export const RECIPES = {
  dialog: { trigger: '[data-open-dialog]', target: '#dlg', close: '#dlg [data-close]:not([data-confirm])', entry: 550, exit: 180 },
  menu: { trigger: '.select[data-pop]', target: '.select[data-pop]', entry: 500, exit: 220 },
  tooltip: { trigger: '.ibtn[data-tip]', target: '#tip', delay: 450, entry: 300, exit: 300 },
  switch: { trigger: '.sw:not(:checked):not(:disabled)', target: '.sw:not(:checked):not(:disabled)', entry: 500, exit: 500 },
};
const FONT_NAMES = {
  'Geist.ttf': ['Geist', '400 700'],
  'BricolageGrotesque.ttf': ['Bricolage Grotesque', '400 800'],
  'GeistMono.ttf': ['Geist Mono', '400 500'],
};
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
const delay = ms => new Promise(done => setTimeout(done, ms));
const quoted = value => JSON.stringify(value);

export function assertReferenceSetup(state, options) {
  const accents = {
    blue: { name: 'Blu', light: '#0B57D0', dark: '#A8C7FA' },
    green: { name: 'Verde', light: '#146C2E', dark: '#6DD58C' },
    red: { name: 'Rosso', light: '#C5221F', dark: '#FFB4AB' },
    yellow: { name: 'Giallo', light: '#F9AB00', dark: '#FDD663' },
  };
  const accent = accents[options.accent];
  const density = options.density === 'touch' ? 'touch' : 'desktop';
  const required = (condition, message) => { if (!condition) throw Error('Reference setup mismatch: ' + message); };
  required(state.dataset.theme === options.scheme && state.dataset.accent === options.accent && state.dataset.density === (options.density === 'touch' ? 'touch' : null), 'root attributes');
  required(state.seedName === accent.name, 'seed label');
  const selected = (buttons, values, value, classes) => buttons.length === values.length && values.every(expected => buttons.some(button => button.value === expected && button.pressed === (expected === value) && (!classes || button.on === (expected === value))));
  required(selected(state.seeds, Object.keys(accents), options.accent, false), 'seed selection');
  required(selected(state.themes, ['system', 'light', 'dark'], options.scheme, true), 'theme selection');
  required(selected(state.densities, ['desktop', 'touch'], density, true), 'density selection');
  const primary = accent[options.scheme].toLowerCase();
  required(state.tokens.primary.toLowerCase() === primary && state.tokens.colorScheme === options.scheme, 'theme/accent tokens');
  const channels = [1, 3, 5].map(start => Number.parseInt(primary.slice(start, start + 2), 16));
  required(state.resolvedPrimaryColor.replace(/\s/g, '') === `rgb(${channels.join(',')})`, 'resolved accent color');
  const metrics = options.density === 'touch' ? ['42px', '42px', '54px', '15px'] : ['34px', '34px', '46px', '14px'];
  required(['controlHeight', 'menuHeight', 'rowHeight', 'fontSize'].every((key, index) => state.tokens[key] === metrics[index]), 'density tokens');
}

export function parseArgs(args) {
  const values = { width: 980, height: 800, dpi: 1, scheme: 'light', motion: 'normal', density: 'compact', accent: 'blue', culture: 'it-IT', recipes: Object.keys(RECIPES) };
  for (let index = 0; index < args.length; index++) {
    const name = args[index];
    if (name === '--validate-only') { values.validateOnly = true; continue; }
    if (!['--browser', '--root', '--manifest', '--out', '--width', '--height', '--dpi', '--scheme', '--motion', '--density', '--accent', '--culture', '--recipes'].includes(name)) throw Error('Unknown argument: ' + name);
    const value = args[++index];
    if (!value || value.startsWith('--')) throw Error('Missing value: ' + name);
    values[name.slice(2)] = ['--width', '--height', '--dpi'].includes(name) ? Number(value) : name === '--recipes' ? value.split(',') : value;
  }
  for (const name of ['width', 'height']) if (!Number.isInteger(values[name]) || values[name] < 100 || values[name] > 8000) throw Error('Invalid viewport: ' + name);
  if (!Number.isFinite(values.dpi) || values.dpi <= 0 || values.dpi > 4) throw Error('Invalid DPI scale.');
  for (const [name, choices] of Object.entries({ scheme: ['light', 'dark'], motion: ['normal', 'reduced'], density: ['compact', 'touch'], accent: ['blue', 'green', 'red', 'yellow'] })) if (!choices.includes(values[name])) throw Error('Invalid ' + name);
  if (!values.recipes.length || values.recipes.some(name => !Object.hasOwn(RECIPES, name)) || new Set(values.recipes).size !== values.recipes.length) throw Error('Unknown or duplicate recipe.');
  if (!values.out) throw Error('--out is required.');
  if (!values.validateOnly && (!values.browser || !isAbsolute(values.browser))) throw Error('Capture requires an explicit absolute --browser path.');
  if (!values.manifest) throw Error('--manifest is required.');
  return values;
}

export async function prepareCopy(root, manifestFile, options) {
  const manifest = JSON.parse(await readFile(manifestFile, 'utf8'));
  if (manifest.schemaVersion !== 1) throw Error('Unsupported manifest version.');
  const sources = [];
  for (const input of manifest.inputs) {
    const path = resolve(root, input.path);
    const bytes = await readFile(path);
    if (sha(bytes).toLowerCase() !== input.sha256.toLowerCase()) throw Error('Pinned input changed: ' + input.path);
    sources.push({ ...input, bytes });
  }
  const html = sources.find(input => input.path === manifest.reference.leaf.path);
  if (!html) throw Error('Pinned Leaf HTML is missing.');
  const fonts = sources.filter(input => Object.keys(FONT_NAMES).some(name => input.path.endsWith('/' + name)));
  if (fonts.length !== 3) throw Error('Exactly three pinned packaged fonts are required.');
  let copy = html.bytes.toString('utf8');
  let replaced = 0;
  copy = copy.replace(/<link\b[^>]*https:\/\/fonts\.(?:googleapis|gstatic)\.com[^>]*>\s*/g, () => { replaced++; return ''; });
  if (replaced !== 3) throw Error('Review changed remote-font links before capture.');
  const faces = fonts.map(input => {
    const [name, weight] = FONT_NAMES[input.path.split('/').at(-1)];
    return `@font-face{font-family:${quoted(name)};font-style:normal;font-weight:${weight};font-display:block;src:url(data:font/ttf;base64,${input.bytes.toString('base64')}) format("truetype")}`;
  }).join('\n');
  const policy = `<meta http-equiv="Content-Security-Policy" content="default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; font-src data:; img-src 'self' data:; connect-src 'none'; object-src 'none'; frame-src 'none'; base-uri 'none'">`;
  copy = copy.replace('<head>', `<head>\n${policy}\n<style id="capture-offline-fonts">${faces}</style>`);
  copy = copy.replace('<html lang="it" data-accent="blue">', `<html lang="it" data-accent="${options.accent}" data-theme="${options.scheme}"${options.density === 'touch' ? ' data-density="touch"' : ''}>`);
  return { copy, manifest, html, fonts, copyHash: sha(copy), sourceHashes: Object.fromEntries(sources.map(({ path, sha256 }) => [path, sha256])), fontHashes: Object.fromEntries(fonts.map(({ path, sha256 }) => [path, sha256])) };
}

class Cdp {
  constructor(socket) {
    this.socket = socket; this.sequence = 0; this.pending = new Map(); this.listeners = new Map();
    socket.addEventListener('message', event => {
      const message = JSON.parse(String(event.data));
      if (message.id) {
        const callback = this.pending.get(message.id); if (!callback) return;
        this.pending.delete(message.id); clearTimeout(callback.timer);
        if (message.error) callback.reject(Error(JSON.stringify(message.error))); else callback.resolve(message.result ?? {});
      } else {
        for (const callback of this.listeners.get(message.method) ?? []) callback(message.params ?? {}, message.sessionId);
      }
    });
    socket.addEventListener('close', () => {
      for (const callback of this.pending.values()) { clearTimeout(callback.timer); callback.reject(Error('CDP connection closed.')); }
      this.pending.clear();
    });
  }
  static async connect(url) {
    const socket = new WebSocket(url);
    await new Promise((done, fail) => { socket.addEventListener('open', done, { once: true }); socket.addEventListener('error', fail, { once: true }); });
    return new Cdp(socket);
  }
  send(method, params = {}, sessionId) {
    const id = ++this.sequence;
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => { this.pending.delete(id); reject(Error('CDP timeout: ' + method)); }, 30000);
      this.pending.set(id, { resolve, reject, timer });
      this.socket.send(JSON.stringify({ id, method, params, ...(sessionId ? { sessionId } : {}) }));
    });
  }
  on(method, callback) {
    const listeners = this.listeners.get(method) ?? new Set(); listeners.add(callback); this.listeners.set(method, listeners);
    return () => listeners.delete(callback);
  }
  once(method, sessionId) {
    return new Promise((done, fail) => {
      let remove;
      const timer = setTimeout(() => { remove(); fail(Error('CDP event timeout: ' + method)); }, 30000);
      remove = this.on(method, (params, source) => { if (source !== sessionId) return; remove(); clearTimeout(timer); done(params); });
    });
  }
}

async function launch(options, output) {
  const profile = await mkdtemp(resolve(output, 'chromium-profile-'));
  const args = ['--headless=new', '--remote-debugging-address=127.0.0.1', '--remote-debugging-port=0', '--user-data-dir=' + profile, '--no-first-run', '--no-default-browser-check', '--disable-background-networking', '--disable-component-update', '--disable-sync', '--disable-extensions', '--disable-quic', '--disable-crash-reporter', '--disable-breakpad', '--metrics-recording-only', '--proxy-server=http://127.0.0.1:9', '--proxy-bypass-list=localhost;127.0.0.1', '--host-resolver-rules=MAP * ~NOTFOUND, EXCLUDE localhost', 'about:blank'];
  const process = spawn(options.browser, args, { windowsHide: true, stdio: ['ignore', 'ignore', 'pipe'] });
  let launchError;
  process.once('error', error => { launchError = error; });
  process.stderr.on('data', bytes => { void appendFile(resolve(output, 'browser-stderr.log'), bytes); });
  let portFile;
  for (let attempt = 0; attempt < 150; attempt++) {
    if (launchError) throw launchError;
    if (process.exitCode !== null) throw Error('Headless browser exited: ' + process.exitCode);
    try { portFile = await readFile(resolve(profile, 'DevToolsActivePort'), 'utf8'); break; } catch { await delay(100); }
  }
  if (!portFile) { process.kill(); throw Error('No DevToolsActivePort after 15 seconds.'); }
  const [port, suffix] = portFile.trim().split(/\r?\n/);
  if (!/^\d+$/.test(port) || !suffix.startsWith('/devtools/browser/')) { process.kill(); throw Error('Invalid local DevTools endpoint.'); }
  const cdp = await Cdp.connect(`ws://127.0.0.1:${port}${suffix}`);
  return { process, cdp, args, profile };
}

async function evaluate(cdp, session, expression) {
  const response = await cdp.send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true }, session);
  if (response.exceptionDetails) throw Error(JSON.stringify(response.exceptionDetails));
  return response.result.value;
}

async function click(cdp, session, selector) {
  const point = await evaluate(cdp, session, `(() => { const e=document.querySelector(${quoted(selector)}); if(!e)throw Error('Missing trigger'); const r=e.getBoundingClientRect(); if(r.width<=0||r.height<=0)throw Error('Hidden trigger'); return {x:r.x+r.width/2,y:r.y+r.height/2}; })()`);
  await cdp.send('Input.dispatchMouseEvent', { type: 'mouseMoved', ...point }, session);
  await cdp.send('Input.dispatchMouseEvent', { type: 'mousePressed', button: 'left', buttons: 1, clickCount: 1, ...point }, session);
  await cdp.send('Input.dispatchMouseEvent', { type: 'mouseReleased', button: 'left', buttons: 0, clickCount: 1, ...point }, session);
}

async function prepareRecipe(cdp, session, file, options, name) {
  const loaded = cdp.once('Page.loadEventFired', session);
  await cdp.send('Emulation.setVirtualTimePolicy', { policy: 'advance' }, session);
  await cdp.send('Animation.setPlaybackRate', { playbackRate: 1 }, session);
  await cdp.send('Page.navigate', { url: pathToFileURL(file).href }, session); await loaded;
  const recipe = RECIPES[name];
  const initial = await evaluate(cdp, session, `(async()=>{
    const select=selector=>{const button=document.querySelector(selector);if(!button)throw Error('Reference setup control is absent: '+selector);button.click();};
    select('.seedbtn[data-accent="'+${quoted(options.accent)}+'"]');
    select('.seg[data-group="theme"] button[data-value="'+${quoted(options.scheme)}+'"]');
    select('.seg[data-group="density"] button[data-value="'+${quoted(options.density === 'touch' ? 'touch' : 'desktop')}+'"]');
    await document.fonts.ready;
    const element=document.querySelector(${quoted(recipe.trigger)}); if(!element)throw Error('Recipe trigger is absent');
    element.setAttribute('data-capture-trigger','true'); element.scrollIntoView({behavior:'instant',block:'center',inline:'center'});
    await new Promise(done=>requestAnimationFrame(()=>requestAnimationFrame(done)));
    const target=${name === 'menu' ? "element.closest('.dd').querySelector(':scope > .menu')" : name === 'switch' ? 'element' : `document.querySelector(${quoted(recipe.target)})`};
    target.setAttribute('data-capture-target','true');
    globalThis.__leafPointerEvents=[];
    for(const type of ['pointerover','pointerout'])document.addEventListener(type,e=>{if(__leafPointerEvents.length<128)__leafPointerEvents.push({type,target:e.target.outerHTML?.slice(0,300),related:e.relatedTarget?.outerHTML?.slice(0,300),x:e.clientX,y:e.clientY});},true);
    for(const animation of document.getAnimations()){
      const timing=animation.effect?.getComputedTiming();
      if(timing&&Number.isFinite(timing.endTime)){try{animation.finish();}catch{animation.pause();}}else animation.pause();
    }
    const probe=new Animation(new KeyframeEffect(document.createElement('span'),[],{duration:1}));
    probe.currentTime=.001;
    if(probe.currentTime!==.001)throw Error('Manual CSS sample grid cannot round-trip at .001 ms');
    probe.cancel();
    const root=document.documentElement,rootStyle=getComputedStyle(root),token=name=>rootStyle.getPropertyValue(name).trim();
    const colors=document.createElement('span');colors.style.color='var(--primary)';document.body.append(colors);const resolvedPrimaryColor=getComputedStyle(colors).color;colors.remove();
    const buttons=(selector,key,classes)=>[...document.querySelectorAll(selector)].map(button=>({value:button.dataset[key],pressed:button.getAttribute('aria-pressed')==='true',...(classes?{on:button.classList.contains('on')}:{})}));
    const setup={dataset:{theme:root.dataset.theme??null,accent:root.dataset.accent??null,density:root.dataset.density??null},seedName:document.querySelector('#seedName').textContent.trim(),seeds:buttons('.seedbtn','accent',false),themes:buttons('.seg[data-group="theme"] button','value',true),densities:buttons('.seg[data-group="density"] button','value',true),tokens:{primary:token('--primary'),colorScheme:rootStyle.colorScheme,controlHeight:token('--h'),menuHeight:token('--h-menu'),rowHeight:token('--h-row'),fontSize:token('--fs')},resolvedPrimaryColor};
    return {clockResolutionMilliseconds:.001,clockBasis:'requested manual CSS sample grid; not a measured frame or JavaScript clock',setup,fonts:[...document.fonts].map(f=>({family:f.family,status:f.status,weight:f.weight,style:f.style})),environment:{width:innerWidth,height:innerHeight,dpiScale:devicePixelRatio,language:navigator.language,intlLocale:Intl.DateTimeFormat().resolvedOptions().locale,documentLanguage:document.documentElement.lang,scheme:matchMedia('(prefers-color-scheme:dark)').matches?'dark':'light',reduced:matchMedia('(prefers-reduced-motion:reduce)').matches}};
  })()`);
  if (initial.environment.width !== options.width || initial.environment.height !== options.height || initial.environment.dpiScale !== options.dpi || initial.environment.scheme !== options.scheme || initial.environment.reduced !== (options.motion === 'reduced') || initial.environment.language !== options.culture || initial.environment.intlLocale !== options.culture) throw Error('Requested browser environment was not applied: ' + JSON.stringify(initial.environment));
  if (initial.fonts.some(font => font.status !== 'loaded')) throw Error('A pinned font face was not loaded.');
  assertReferenceSetup(initial.setup, options);
  await cdp.send('Animation.setPlaybackRate', { playbackRate: 0 }, session);
  await cdp.send('Emulation.setVirtualTimePolicy', { policy: 'pause' }, session);
  return initial;
}

async function trigger(cdp, session, name, phase) {
  await evaluate(cdp, session, `globalThis.__leafPrior=new Map(document.querySelector('[data-capture-target]').getAnimations({subtree:true}).map(a=>[a,a.currentTime??0]))`);
  if (name === 'tooltip') {
    if (phase === 'enter') {
      const point = await evaluate(cdp, session, `(()=>{const r=document.querySelector('[data-capture-trigger]').getBoundingClientRect();return {x:r.x+Math.min(4,r.width/4),y:r.y+r.height/2}})()`);
      await cdp.send('Input.dispatchMouseEvent', { type: 'mouseMoved', ...point }, session);
      const expired = cdp.once('Emulation.virtualTimeBudgetExpired', session);
      await cdp.send('Emulation.setVirtualTimePolicy', { policy: 'advance', budget: RECIPES.tooltip.delay, maxVirtualTimeTaskStarvationCount: 1000 }, session);
      await expired;
      await cdp.send('Emulation.setVirtualTimePolicy', { policy: 'pause' }, session);
    } else await cdp.send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: 0, y: 0 }, session);
  } else await click(cdp, session, name === 'dialog' && phase !== 'enter' ? RECIPES.dialog.close : '[data-capture-trigger]');
  return evaluate(cdp, session, `(()=>{
    const target=document.querySelector('[data-capture-target]'); void getComputedStyle(target).transform;
    const rect=target.getBoundingClientRect(),style=getComputedStyle(target),enter=${phase === 'enter'},recipe=${quoted(name)};
    const closedReducedDialog=recipe==='dialog'&&!enter&&matchMedia('(prefers-reduced-motion:reduce)').matches;
    if(!closedReducedDialog&&(rect.width<=0||rect.height<=0||rect.right<=0||rect.bottom<=0||rect.left>=innerWidth||rect.top>=innerHeight||style.display==='none'))throw Error('Recipe target is empty or outside the client');
    if(recipe==='dialog'&&(closedReducedDialog?(target.open||target.matches(':modal')):(!target.open||!target.matches(':modal')||(enter&&target.classList.contains('closing'))||(!enter&&!target.classList.contains('closing')))))throw Error('Dialog handler did not reach its expected state');
    if(recipe==='menu'&&target.classList.contains('open')!==enter)throw Error('Menu handler did not reach its expected state');
    if(recipe==='tooltip'&&(target.classList.contains('show')!==enter||!target.textContent.trim()))throw Error('Tooltip handler did not reach its expected state');
    if(recipe==='switch'&&target.checked!==enter)throw Error('Native switch activation did not reach its expected state');
    globalThis.__leafExpected={recipe,enter,closedReducedDialog};
    globalThis.__leafAnimations=target.getAnimations({subtree:true}).filter(a=>Number.isFinite(a.effect?.getComputedTiming().endTime)&&(recipe!=='dialog'||['dIn','dOut','fade'].includes(a.animationName)));
    globalThis.__leafBases=new Map(__leafAnimations.map(a=>[a,__leafPrior.get(a)??0]));
    for(const animation of __leafAnimations){animation.pause();animation.currentTime=__leafBases.get(animation);}
    return __leafAnimations.map(a=>({type:a.constructor.name,property:a.transitionProperty??null,name:a.animationName??null,pseudoElement:a.effect.pseudoElement??null,carried:__leafPrior.has(a),sampleBaseTimeMilliseconds:__leafBases.get(a),timing:a.effect.getTiming(),computedTiming:a.effect.getComputedTiming(),keyframes:a.effect.getKeyframes()}));
  })()`);
}

async function drainEvents(cdp, session) {
  const expired = cdp.once('Emulation.virtualTimeBudgetExpired', session);
  await cdp.send('Emulation.setVirtualTimePolicy', { policy: 'advance', budget: 32, maxVirtualTimeTaskStarvationCount: 1000 }, session);
  await expired;
  await cdp.send('Emulation.setVirtualTimePolicy', { policy: 'pause' }, session);
}

async function seek(cdp, session, time) {
  return evaluate(cdp, session, `(()=>{
    for(const animation of globalThis.__leafAnimations??[]){animation.currentTime=Math.min((__leafBases.get(animation)??0)+${time},animation.effect.getComputedTiming().endTime);}
    const target=document.querySelector('[data-capture-target]');const style=getComputedStyle(target),rect=target.getBoundingClientRect();
    if(__leafExpected.recipe==='tooltip'&&target.classList.contains('show')!==__leafExpected.enter)throw Error('Tooltip source state changed during manual CSS sampling: '+JSON.stringify(globalThis.__leafPointerEvents));
    const declaration=s=>Object.fromEntries(['opacity','transform','clip-path','border-radius','background-color','transition-duration','transition-delay','transition-timing-function','animation-duration','animation-timing-function','visibility'].map(name=>[name,s.getPropertyValue(name)]));
    const state={rect:{x:rect.x,y:rect.y,width:rect.width,height:rect.height},style:declaration(style),before:declaration(getComputedStyle(target,'::before')),after:declaration(getComputedStyle(target,'::after')),backdrop:target.tagName==='DIALOG'?declaration(getComputedStyle(target,'::backdrop')):null,attributes:Object.fromEntries([...target.attributes].map(a=>[a.name,a.value])),checked:target.checked??null,open:target.open??null,focus:document.activeElement?.outerHTML.slice(0,500),pointerEvents:globalThis.__leafPointerEvents,animations:(globalThis.__leafAnimations??[]).map(a=>({currentTime:a.currentTime,playState:a.playState,pending:a.pending,timing:a.effect.getComputedTiming()}))};
    return state;
  })()`);
}

export async function run(options) {
  const root = resolve(options.root ?? resolve(dirname(fileURLToPath(import.meta.url)), '../..'));
  const output = resolve(root, options.out);
  const within = relative(root, output);
  if (!within || within === '..' || within.startsWith('..' + sep) || isAbsolute(within) || !within.startsWith('artifacts' + sep)) throw Error('Output must be a new directory under the repository artifacts folder.');
  try { if ((await readdir(output)).length) throw Error('Output directory must be empty; keep previous capture evidence intact.'); }
  catch (error) { if (error.code !== 'ENOENT') throw error; }
  await mkdir(output, { recursive: true });
  const input = await prepareCopy(root, resolve(root, options.manifest), options);
  const file = resolve(output, 'reference.offline.html'); await writeFile(file, input.copy);
  const harnessHash = sha(await readFile(fileURLToPath(import.meta.url)));
  const report = { schemaVersion: 1, status: options.validateOnly ? 'inputs-validated-capture-not-run' : 'capture-in-progress', inputHashes: input.sourceHashes, offlineCopyHash: input.copyHash, harnessHash, options, captures: [], network: [], javascriptErrors: [], limitations: ['Manual CSS currentTime samples are not delivered JavaScript/render-frame timestamps.', 'Manual CSS seeks use declared 32 ms virtual-event drains; animationend callbacks have no delivered-frame timestamp. Before/after screenshot state records source lifecycle changes.', 'The original Google Fonts response is replaced only in the ignored copy by the three pinned packaged font byte streams.', 'Full-client headless Chromium PNGs are reference candidates, not native Nazor/WinUI acceptance or a cross-renderer parity pass.'] };
  await writeFile(resolve(output, 'run.json'), JSON.stringify(report, null, 2));
  if (options.validateOnly) { console.log('Pinned input hashes and offline copy validated; no browser was launched.'); return report; }
  let browser;
  try { browser = await launch(options, output); }
  catch (error) { report.status = 'failed'; report.error = String(error); await writeFile(resolve(output, 'run.json'), JSON.stringify(report, null, 2)); throw error; }
  const { cdp } = browser;
  try {
    report.browser = await cdp.send('Browser.getVersion');
    report.browserBinaryHash = sha(await readFile(options.browser));
    report.browserArguments = browser.args;
    const { targetId } = await cdp.send('Target.createTarget', { url: 'about:blank' });
    const { sessionId } = await cdp.send('Target.attachToTarget', { targetId, flatten: true });
    for (const name of ['Page', 'Runtime', 'Network', 'Animation']) await cdp.send(name + '.enable', {}, sessionId);
    await cdp.send('Network.setBlockedURLs', { urls: ['http://*', 'https://*', 'ws://*', 'wss://*', 'ftp://*'] }, sessionId);
    cdp.on('Fetch.requestPaused', (event, source) => {
      if (source !== sessionId) return;
      const allowed = event.request.url === pathToFileURL(file).href || event.request.url.startsWith('data:');
      report.network.push({ url: event.request.url.startsWith('data:') ? 'data:[inline]' : event.request.url, allowed });
      void cdp.send(allowed ? 'Fetch.continueRequest' : 'Fetch.failRequest', { requestId: event.requestId, ...(allowed ? {} : { errorReason: 'BlockedByClient' }) }, sessionId).catch(error => report.javascriptErrors.push(String(error)));
    });
    cdp.on('Runtime.exceptionThrown', (event, source) => { if (source === sessionId) report.javascriptErrors.push(event.exceptionDetails); });
    await cdp.send('Fetch.enable', { patterns: [{ urlPattern: '*', requestStage: 'Request' }] }, sessionId);
    await cdp.send('Emulation.setDeviceMetricsOverride', { width: options.width, height: options.height, deviceScaleFactor: options.dpi, mobile: false, screenWidth: options.width, screenHeight: options.height }, sessionId);
    await cdp.send('Emulation.setLocaleOverride', { locale: options.culture }, sessionId);
    await cdp.send('Emulation.setUserAgentOverride', { userAgent: report.browser.userAgent, acceptLanguage: options.culture }, sessionId);
    await cdp.send('Emulation.setTimezoneOverride', { timezoneId: 'UTC' }, sessionId);
    await cdp.send('Emulation.setEmulatedMedia', { media: 'screen', features: [{ name: 'prefers-color-scheme', value: options.scheme }, { name: 'prefers-reduced-motion', value: options.motion === 'reduced' ? 'reduce' : 'no-preference' }, { name: 'forced-colors', value: 'none' }] }, sessionId);
    const revision = execFileSync('git', ['-C', root, 'rev-parse', 'HEAD'], { encoding: 'utf8', windowsHide: true }).trim();
    for (const name of options.recipes) {
      for (const sequence of ['enter', 'exit', 'reverse-at-half-entry']) {
        const environment = await prepareRecipe(cdp, sessionId, file, options, name);
        let animations = await trigger(cdp, sessionId, name, 'enter');
        if (options.motion === 'normal' && !animations.some(animation => animation.computedTiming.endTime > 0)) throw Error('No nonzero normal CSS animation was generated for ' + name);
        const entryDuration = Math.max(0, ...animations.map(a => a.computedTiming.endTime - a.sampleBaseTimeMilliseconds));
        if (sequence !== 'enter') {
          await seek(cdp, sessionId, sequence === 'exit' ? entryDuration : entryDuration / 2);
          await drainEvents(cdp, sessionId);
          animations = await trigger(cdp, sessionId, name, 'exit');
          if (options.motion === 'normal' && !animations.some(animation => animation.computedTiming.endTime > 0)) throw Error('No nonzero normal CSS exit was generated for ' + name + ': ' + sequence);
        }
        const duration = Math.max(0, ...animations.map(a => a.computedTiming.endTime - a.sampleBaseTimeMilliseconds));
        if (options.motion === 'normal' && duration <= 0) throw Error('Normal phase has no remaining CSS duration: ' + name + ': ' + sequence);
        for (const fraction of FRACTIONS) {
          const time = Math.round(duration * fraction * 1000) / 1000;
          await seek(cdp, sessionId, time);
          await drainEvents(cdp, sessionId);
          const state = await seek(cdp, sessionId, time);
          const prefix = `${name}-${sequence}-${Math.round(fraction * 100)}`;
          const origin = await evaluate(cdp, sessionId, '({x:scrollX,y:scrollY})');
          const screenshotViewport = { ...origin, width: options.width, height: options.height, scale: 1 };
          const screenshot = await cdp.send('Page.captureScreenshot', { format: 'png', fromSurface: true, captureBeyondViewport: false, optimizeForSpeed: false, clip: screenshotViewport }, sessionId);
          const afterScreenshot = await seek(cdp, sessionId, time);
          const lifecycleChanged = JSON.stringify(state.attributes) !== JSON.stringify(afterScreenshot.attributes) || JSON.stringify(state.rect) !== JSON.stringify(afterScreenshot.rect);
          if (lifecycleChanged) (report.lifecycleBoundaries ??= []).push(prefix);
          const sourceTargetPresent = state.rect.width > 0 && state.rect.height > 0 && state.style.visibility !== 'hidden';
          const bytes = Buffer.from(screenshot.data, 'base64');
          if (!bytes.subarray(0, 8).equals(Buffer.from([137, 80, 78, 71, 13, 10, 26, 10])) || bytes.readUInt32BE(16) !== Math.round(options.width * options.dpi) || bytes.readUInt32BE(20) !== Math.round(options.height * options.dpi)) throw Error('Screenshot is not the requested lossless physical client size.');
          await writeFile(resolve(output, prefix + '.png'), bytes);
          const descriptor = {
            image: prefix + '.png', scenario: `leaf-${name}-${sequence}-${Math.round(fraction * 100)}pct`,
            platformBuild: `${os.platform()} ${os.release()} ${os.arch()}`, dpiScale: options.dpi, clientSize: [options.width, options.height], units: 'logical-pixels',
            fontHashes: input.fontHashes, culture: options.culture, theme: 'NazorLeaf', colorScheme: options.scheme, accent: options.accent, density: options.density,
            contrast: 'normal', input: 'pointer', motion: options.motion, timeMilliseconds: time,
            renderer: `${report.browser.product}; ${report.browser.revision}; headless`, sourceRevision: revision,
            referenceVersion: input.html.sha256, clockResolutionMilliseconds: environment.clockResolutionMilliseconds,
            capture: {
              policy: 'manual-css-time-not-javascript-frame-time', screenshotViewport, javascriptDrainBudgetMilliseconds: 32,
              effectScope: 'dialog dIn/dOut/fade keyframes; other recipe target/pseudo effects; close-button descendants excluded',
              lifecycleChangedDuringScreenshot: lifecycleChanged, sourceTargetPresent,
              candidateStatus: lifecycleChanged || !sourceTargetPresent ? 'source-lifecycle-boundary-review-required' : 'manual-css-candidate',
              fraction, phaseDurationMilliseconds: duration, nominalSourceEntryMilliseconds: RECIPES[name].entry,
              nominalSourceExitMilliseconds: RECIPES[name].exit, triggerDelayMilliseconds: RECIPES[name].delay ?? 0,
              offlineCopyHash: input.copyHash, browserBinaryHash: report.browserBinaryHash, harnessHash, imageHash: sha(bytes),
              environment, animations, state, afterScreenshot,
            },
          };
          await writeFile(resolve(output, prefix + '.json'), JSON.stringify(descriptor, null, 2));
          report.captures.push({ descriptor: prefix + '.json', imageHash: sha(bytes) });
        }
      }
    }
    if (report.javascriptErrors.length) throw Error('Runtime errors occurred; inspect run.json.');
    report.status = 'manual-css-captures-produced-native-acceptance-pending';
  } catch (error) {
    report.status = 'failed'; report.error = String(error); throw error;
  } finally {
    await writeFile(resolve(output, 'run.json'), JSON.stringify(report, null, 2));
    try { await cdp.send('Browser.close'); } catch { browser.process.kill(); }
    cdp.socket.close();
  }
  return report;
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try { await run(parseArgs(process.argv.slice(2))); }
  catch (error) { console.error(String(error)); process.exitCode = 1; }
}
