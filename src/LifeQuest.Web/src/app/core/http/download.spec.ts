import { fileNameFrom } from './download';

describe('fileNameFrom', () => {
  it('reads plain and RFC 5987 file names', () => {
    expect(fileNameFrom('attachment; filename=lifequest-verilerim-20260925.json', 'x')).toBe('lifequest-verilerim-20260925.json');
    expect(fileNameFrom('attachment; filename="plan.ics"', 'x')).toBe('plan.ics');
    expect(fileNameFrom("attachment; filename=a.ics; filename*=UTF-8''g%C3%B6rev.ics", 'x')).toBe('görev.ics');
  });

  it('falls back when the header is missing', () => {
    expect(fileNameFrom(null, 'lifequest.json')).toBe('lifequest.json');
  });
});
