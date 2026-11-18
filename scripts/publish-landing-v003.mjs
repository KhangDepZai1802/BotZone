import assert from 'node:assert/strict';
import { mkdir, writeFile } from 'node:fs/promises';
import { base, client } from '../tests/web/live-api.mjs';

// Existing content-staff API, CSRF and revision checks; no direct database access.
assert.ok(['localhost', '127.0.0.1'].includes(new URL(base).hostname), 'Local BotZone only.');
const content = await client('content@botzone.local');
try {
  const state = await content.get('/api/admin/landing');
  const published = await content.get('/api/landing');
  assert.deepEqual(state.draft.sections, published.sections,
    'An unpublished draft exists. Preserve it and resolve the content conflict before publishing.');
  const sections = structuredClone(state.draft.sections);
  let matched = 0;
  for (const section of sections) {
    if (section.type === 'hero' && section.productSlug === 'iphone-18-pro-max') {
      section.model = 'v003:iphone-18-pro-max';
      section.poster = '/models/v003/iphone-18-pro-max.webp';
      section.fold = 0;
      section.colors = ['#54393E', '#27282B', '#C5C7C9', '#AFBEC8'];
      matched++;
    }
    if (section.type === 'duo' && section.productSlug === 'iphone-duo') {
      section.model = 'v003:iphone-duo';
      section.poster = '/models/v003/iphone-duo.webp';
      section.fold = 180;
      section.colors = ['#2B3440', '#DBDBD4'];
      matched++;
    }
  }
  assert.equal(matched, 2, 'Expected the two existing hero sections.');
  await mkdir('artifacts/3d-v003', { recursive: true });
  const plan = { at: new Date().toISOString(), revision: state.draft.revision,
    before: published, sections, preservedHistory: state.history };
  await writeFile('artifacts/3d-v003/landing-plan.json', JSON.stringify(plan, null, 2));
  if (process.argv.includes('--apply')) {
    if (JSON.stringify(sections) === JSON.stringify(state.draft.sections)) {
      console.log(JSON.stringify({ alreadyApplied: true, revision: state.draft.revision }));
    } else {
      const saved = await content.command('save_landing', { revision: state.draft.revision, sections });
      const result = await content.command('publish_landing', { revision: saved.revision });
      const current = await content.get('/api/landing');
      assert.deepEqual(current.sections, sections);
      await writeFile('artifacts/3d-v003/landing-published.json', JSON.stringify({ at: new Date().toISOString(), ...result, sections }, null, 2));
      await writeFile('data/landing-demo.json', JSON.stringify(sections, null, 2) + '\n');
      console.log(JSON.stringify({ published: true, ...result, sectionCount: sections.length }));
    }
  } else console.log(JSON.stringify({ planned: true, revision: state.draft.revision, matched, sectionCount: sections.length }));
  await content.post('/api/auth/logout', {});
} finally {
  await content.dispose();
}
