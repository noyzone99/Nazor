import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile, writeFile, mkdtemp, mkdir } from 'node:fs/promises';
import { relative, resolve, sep } from 'node:path';
import { parseArgs, prepareCopy, run, assertReferenceSetup, FRACTIONS, RECIPES } from '../../Scripts/Tests/capture-leaf-reference.mjs';

const root = process.cwd();
let manifest, referenceFile;
const defaults = () => parseArgs(['--manifest', manifest, '--out', 'artifacts/example', '--validate-only']);

test.before(async () => {
  await mkdir(resolve(root, 'artifacts'), { recursive: true });
  const folder = await mkdtemp(resolve(root, 'artifacts/leaf-capture-inputs-'));
  referenceFile = resolve(folder, 'reference.html');
  const referencePath = relative(root, referenceFile).split(sep).join('/');
  await writeFile(referenceFile, `<html lang="it" data-accent="blue">
<head>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Geist">
</head>
<body>Test reference</body>
</html>`);
  const fonts = ['Geist.ttf', 'BricolageGrotesque.ttf', 'GeistMono.ttf']
    .map(name => 'Sources/Themes/Nazor.Themes.NazorLeaf/Fonts/' + name);
  const inputs = await Promise.all([referencePath, ...fonts].map(async path => ({
    path,
    sha256: createHash('sha256').update(await readFile(resolve(root, path))).digest('hex'),
  })));
  manifest = resolve(folder, 'manifest.json');
  await writeFile(manifest, JSON.stringify({ schemaVersion: 1, reference: { leaf: { path: referencePath } }, inputs }));
});

const blueSetup = () => ({
  dataset: { theme: 'light', accent: 'blue', density: null }, seedName: 'Blu',
  seeds: ['blue', 'red', 'yellow', 'green'].map(value => ({ value, pressed: value === 'blue' })),
  themes: ['system', 'light', 'dark'].map(value => ({ value, pressed: value === 'light', on: value === 'light' })),
  densities: ['desktop', 'touch'].map(value => ({ value, pressed: value === 'desktop', on: value === 'desktop' })),
  tokens: { primary: '#0B57D0', colorScheme: 'light', controlHeight: '34px', menuHeight: '34px', rowHeight: '46px', fontSize: '14px' },
  resolvedPrimaryColor: 'rgb(11, 87, 208)',
});

test('setup validation accepts coherent blue desktop and red dark touch controls and tokens', () => {
  assertReferenceSetup(blueSetup(), defaults());
  const state = blueSetup();
  state.dataset = { theme: 'dark', accent: 'red', density: 'touch' }; state.seedName = 'Rosso';
  for (const button of state.seeds) button.pressed = button.value === 'red';
  for (const button of state.themes) button.pressed = button.on = button.value === 'dark';
  for (const button of state.densities) button.pressed = button.on = button.value === 'touch';
  state.tokens = { primary: '#FFB4AB', colorScheme: 'dark', controlHeight: '42px', menuHeight: '42px', rowHeight: '54px', fontSize: '15px' };
  state.resolvedPrimaryColor = 'rgb(255, 180, 171)';
  assertReferenceSetup(state, { ...defaults(), scheme: 'dark', accent: 'red', density: 'touch' });
});

test('injected root attributes cannot pass with stale System or seed button state', () => {
  const state = blueSetup();
  for (const button of state.themes) button.pressed = button.on = button.value === 'system';
  assert.throws(() => assertReferenceSetup(state, defaults()), /theme selection/);
  const seed = blueSetup(); seed.seeds[1].pressed = true;
  assert.throws(() => assertReferenceSetup(seed, defaults()), /seed selection/);
  const label = blueSetup(); label.seedName = 'Rosso';
  assert.throws(() => assertReferenceSetup(label, defaults()), /seed label/);
});

test('setup validation rejects visually stale density or accent tokens independently of selected controls', () => {
  const density = blueSetup(); density.tokens.rowHeight = '54px';
  assert.throws(() => assertReferenceSetup(density, defaults()), /density tokens/);
  const accent = blueSetup(); accent.tokens.primary = '#C5221F';
  assert.throws(() => assertReferenceSetup(accent, defaults()), /theme\/accent tokens/);
  const resolved = blueSetup(); resolved.resolvedPrimaryColor = 'rgb(0, 0, 0)';
  assert.throws(() => assertReferenceSetup(resolved, defaults()), /resolved accent color/);
});

test('setup validation requires consistent selected classes, density controls and root attributes', () => {
  const classes = blueSetup(); classes.themes[1].on = false;
  assert.throws(() => assertReferenceSetup(classes, defaults()), /theme selection/);
  const controls = blueSetup(); controls.densities[0].on = false;
  assert.throws(() => assertReferenceSetup(controls, defaults()), /density selection/);
  const attributes = blueSetup(); attributes.dataset.density = 'touch';
  assert.throws(() => assertReferenceSetup(attributes, defaults()), /root attributes/);
});

test('CLI requires an explicit absolute executable path for actual captures', () => {
  assert.throws(() => parseArgs(['--out', 'artifacts/example']), /explicit absolute/);
  assert.throws(() => parseArgs(['--out', 'artifacts/example', '--browser', 'chrome.exe']), /explicit absolute/);
  assert.throws(() => parseArgs(['--out', 'artifacts/example', '--validate-only', '--dpi', 'NaN']), /Invalid DPI/);
  assert.throws(() => parseArgs(['--out', 'artifacts/example', '--validate-only', '--width', '980.5']), /Invalid viewport/);
  assert.throws(() => parseArgs(['--out', 'artifacts/example', '--validate-only', '--recipes', 'menu,menu']), /duplicate recipe/);
  assert.throws(() => parseArgs(['--out', 'artifacts/example', '--validate-only', '--scheme', 'system']), /Invalid scheme/);
  assert.throws(() => parseArgs(['--out', 'artifacts/example', '--validate-only']), /--manifest is required/);
  assert.throws(() => parseArgs(['--out', 'artifacts/example', '--bogus']), /Unknown argument/);
});

test('offline copy pins all bytes, substitutes only the three font links and preserves the supplied HTML file', async () => {
  const before = await readFile(referenceFile);
  const prepared = await prepareCopy(root, manifest, { ...defaults(), scheme: 'dark', accent: 'red', density: 'touch' });
  assert.equal(Object.keys(prepared.sourceHashes).length, 4);
  assert.equal(Object.keys(prepared.fontHashes).length, 3);
  assert.equal((prepared.copy.match(/@font-face\{/g) ?? []).length, 3);
  assert.equal((prepared.copy.match(/data:font\/ttf;base64,/g) ?? []).length, 3);
  assert.doesNotMatch(prepared.copy, /<link\b[^>]*https:\/\/fonts\./);
  assert.match(prepared.copy, /data-accent="red" data-theme="dark" data-density="touch"/);
  assert.match(prepared.copy, /connect-src 'none'/);
  assert.deepEqual(await readFile(referenceFile), before);
});

test('changed pinned input bytes and missing packaged faces fail before a browser is needed', async () => {
  const folder = await mkdtemp(resolve(root, 'artifacts/leaf-capture-contract-'));
  const original = JSON.parse(await readFile(manifest, 'utf8'));
  const changed = structuredClone(original); changed.inputs[0].sha256 = '0'.repeat(64);
  const changedFile = resolve(folder, 'changed.json'); await writeFile(changedFile, JSON.stringify(changed));
  await assert.rejects(() => prepareCopy(root, changedFile, defaults()), /Pinned input changed/);
  const missing = structuredClone(original); missing.inputs.pop();
  const missingFile = resolve(folder, 'missing.json'); await writeFile(missingFile, JSON.stringify(missing));
  await assert.rejects(() => prepareCopy(root, missingFile, defaults()), /Exactly three/);
});

test('all four selected source recipes include endpoints and quarter points', () => {
  assert.deepEqual(Object.keys(RECIPES), ['dialog', 'menu', 'tooltip', 'switch']);
  assert.deepEqual(FRACTIONS, [0, 0.25, 0.5, 0.75, 1]);
  assert.equal(RECIPES.dialog.exit, 180);
  assert.equal(RECIPES.menu.entry, 500);
  assert.equal(RECIPES.tooltip.delay, 450);
  assert.equal(RECIPES.switch.entry, 500);
});

test('validation mode writes complete pending provenance but rejects protected or existing output paths', async () => {
  await assert.rejects(() => run({ ...defaults(), root, out: 'Docs/capture-output' }), /under the repository artifacts/);
  const folder = await mkdtemp(resolve(root, 'artifacts/leaf-capture-validated-'));
  const result = await run({ ...defaults(), root, out: folder });
  assert.equal(result.status, 'inputs-validated-capture-not-run');
  assert.equal(result.captures.length, 0);
  assert.equal(result.browser, undefined);
  assert.equal(result.offlineCopyHash.length, 64);
  assert.equal(result.harnessHash.length, 64);
  assert.equal(Object.keys(result.inputHashes).length, 4);
  assert.equal(JSON.parse(await readFile(resolve(folder, 'run.json'), 'utf8')).status, result.status);
  await assert.rejects(() => run({ ...defaults(), root, out: folder }), /must be empty/);
});
