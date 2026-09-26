import { section } from '../dictionary';

export const ui = section(
  {
    interestLegendOnce: 'Bir kez dokun: ilgimi çekiyor',
    interestLegendTwice: 'İki kez: çok seviyorum',
    interestLove: 'çok seviyorum',
    interestLike: 'ilgimi çekiyor',
    interestNone: 'seçili değil',
    closeToast: 'Bildirimi kapat',
    rating: 'Deneyim puanı',
    stars: (n: number) => `${n} yıldız`,
    progress: 'İlerleme',
  },
  {
    interestLegendOnce: 'Tap once: interested',
    interestLegendTwice: 'Twice: love it',
    interestLove: 'love it',
    interestLike: 'interested',
    interestNone: 'not selected',
    closeToast: 'Dismiss notification',
    rating: 'Experience rating',
    stars: (n: number) => `${n} ${n === 1 ? 'star' : 'stars'}`,
    progress: 'Progress',
  },
);
