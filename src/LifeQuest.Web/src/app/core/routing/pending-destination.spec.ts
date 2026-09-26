import { consumeDestination, isSafeInternalUrl, rememberDestination } from './pending-destination';
import { partyInviteUrl, partyPath } from './app-paths';

describe('pending destination', () => {
  beforeEach(() => sessionStorage.clear());

  it('keeps an internal invite link once', () => {
    rememberDestination('/party/ABCD234567');
    expect(consumeDestination()).toBe('/party/ABCD234567');
    expect(consumeDestination()).toBeNull();
  });

  it('never stores external or protocol-relative urls', () => {
    for (const url of ['https://evil.example', '//evil.example', '/\\evil.example', '', null]) {
      expect(isSafeInternalUrl(url)).toBe(false);
      rememberDestination(url);
      expect(consumeDestination()).toBeNull();
    }
  });

  it('builds the shareable invite link from the app origin', () => {
    expect(partyPath('ABC')).toEqual(['/party', 'ABC']);
    expect(partyInviteUrl('ABC', 'https://lifequesttest.gvnaitech.com')).toBe('https://lifequesttest.gvnaitech.com/party/ABC');
  });
});
