import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

interface IconShape {
  paths?: string[];
  circles?: [number, number, number][];
  rects?: [number, number, number, number, number][];
  transform?: string;
}

const THUMB = 'M7 11v9H4v-9z M7 11l4-7.5a2 2 0 0 1 3.2 2.1L13 10.5h5.6a2 2 0 0 1 2 2.4l-1.3 5.6a2 2 0 0 1-2 1.5H7';

/** Kütüphanesiz, 24×24 çizgi ikon seti (stroke tabanlı, currentColor). */
const ICONS = {
  compass: { circles: [[12, 12, 9]], paths: ['M15.5 8.5 13.2 13.2 8.5 15.5 10.8 10.8z'] },
  landmark: { paths: ['M3 21h18', 'M4 10h16', 'M12 3 3.5 8h17z', 'M6 10v8', 'M10 10v8', 'M14 10v8', 'M18 10v8'] },
  book: { paths: ['M5 4h11a3 3 0 0 1 3 3v13H8a3 3 0 0 1-3-3z', 'M5 17a3 3 0 0 1 3-3h11'] },
  users: { circles: [[9, 8, 3.5], [17, 9, 2.5]], paths: ['M2.5 20a6.5 6.5 0 0 1 13 0', 'M17 14.5a5 5 0 0 1 4.5 5'] },
  activity: { paths: ['M3 12h4l3-8 4 16 3-8h4'] },
  palette: {
    paths: ['M12 3a9 9 0 1 0 0 18c1.1 0 2-.8 2-1.8 0-.5-.2-.9-.5-1.2a1.7 1.7 0 0 1 1.3-3H17a4 4 0 0 0 4-4c0-4.4-4-8-9-8z'],
    circles: [[7.5, 11, 1], [10, 7, 1], [14.5, 7, 1]],
  },
  leaf: { paths: ['M5 19c0-8 5-14 15-15-1 10-7 15-15 15z', 'M5 19l8-8'] },
  sparkles: {
    paths: [
      'M12 3l1.8 4.7 4.7 1.8-4.7 1.8L12 16l-1.8-4.7-4.7-1.8 4.7-1.8z',
      'M19 15l.8 2.2 2.2.8-2.2.8L19 21l-.8-2.2L16 18l2.2-.8z',
    ],
  },
  sun: {
    circles: [[12, 12, 4]],
    paths: ['M12 2v2', 'M12 20v2', 'M4.9 4.9l1.4 1.4', 'M17.7 17.7l1.4 1.4', 'M2 12h2', 'M20 12h2', 'M4.9 19.1l1.4-1.4', 'M17.7 6.3l1.4-1.4'],
  },
  moon: { paths: ['M20 14.5A8 8 0 1 1 9.5 4a6.5 6.5 0 0 0 10.5 10.5z'] },
  monitor: { rects: [[3, 4, 18, 12, 2]], paths: ['M8 20h8', 'M12 16v4'] },
  check: { paths: ['M5 12.5l5 5L19 7'] },
  x: { paths: ['M6 6l12 12', 'M18 6 6 18'] },
  clock: { circles: [[12, 12, 9]], paths: ['M12 7v5l3 2'] },
  coins: { circles: [[12, 12, 9]], paths: ['M14.5 9h-4a1.5 1.5 0 0 0 0 3h3a1.5 1.5 0 0 1 0 3h-4', 'M12 7.5V9', 'M12 15v1.5'] },
  star: { paths: ['M12 3l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z'] },
  zap: { paths: ['M13 2 4 14h7l-1 8 9-12h-7z'] },
  'arrow-left': { paths: ['M19 12H5', 'M11 6l-6 6 6 6'] },
  'chevron-right': { paths: ['M9 6l6 6-6 6'] },
  trophy: { paths: ['M8 4h8v5a4 4 0 0 1-8 0z', 'M8 6H5a3 3 0 0 0 3 5', 'M16 6h3a3 3 0 0 1-3 5', 'M12 13v4', 'M8 21h8', 'M9.5 17h5l.5 4h-6z'] },
  list: { paths: ['M10 6h10', 'M10 12h10', 'M10 18h10', 'M3.5 6l1.5 1.5L7.5 5', 'M3.5 12l1.5 1.5 2.5-2.5', 'M3.5 18l1.5 1.5 2.5-2.5'] },
  user: { circles: [[12, 8, 4]], paths: ['M4 21a8 8 0 0 1 16 0'] },
  lock: { rects: [[5, 11, 14, 10, 2]], paths: ['M8 11V8a4 4 0 0 1 8 0v3'] },
  logout: { paths: ['M15 4h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-3', 'M10 8l-4 4 4 4', 'M6 12h10'] },
  trash: { paths: ['M4 7h16', 'M9 7V4h6v3', 'M6 7l1 13h10l1-13', 'M10 11v6', 'M14 11v6'] },
  'map-pin': { paths: ['M12 21s7-6.2 7-12a7 7 0 0 0-14 0c0 5.8 7 12 7 12z'], circles: [[12, 9, 2.5]] },
  'thumbs-up': { paths: [THUMB] },
  'thumbs-down': { paths: [THUMB], transform: 'rotate(180 12 12)' },
  refresh: { paths: ['M20 11a8 8 0 0 0-14.5-4.5L4 8', 'M4 4v4h4', 'M4 13a8 8 0 0 0 14.5 4.5L20 16', 'M20 20v-4h-4'] },
  plus: { paths: ['M12 5v14', 'M5 12h14'] },
  info: { circles: [[12, 12, 9]], paths: ['M12 11v5', 'M12 8h.01'] },
  flag: { paths: ['M5 21V4', 'M5 4h11l-2 4 2 4H5'] },
  heart: { paths: ['M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10z'] },
  target: { circles: [[12, 12, 9], [12, 12, 5], [12, 12, 1]] },
  hourglass: { paths: ['M7 3h10', 'M7 21h10', 'M8 3v3l4 5 4-5V3', 'M8 21v-3l4-5 4 5v3'] },
} satisfies Record<string, IconShape>;

export type IconName = keyof typeof ICONS;

@Component({
  selector: 'lq-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { 'aria-hidden': 'true' },
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      [attr.stroke-width]="strokeWidth()"
      stroke-linecap="round"
      stroke-linejoin="round"
    >
      <g [attr.transform]="shape().transform ?? null">
        @for (d of shape().paths ?? []; track $index) {
          <path [attr.d]="d" />
        }
        @for (c of shape().circles ?? []; track $index) {
          <circle [attr.cx]="c[0]" [attr.cy]="c[1]" [attr.r]="c[2]" />
        }
        @for (r of shape().rects ?? []; track $index) {
          <rect [attr.x]="r[0]" [attr.y]="r[1]" [attr.width]="r[2]" [attr.height]="r[3]" [attr.rx]="r[4]" />
        }
      </g>
    </svg>
  `,
  styles: `
    :host {
      display: inline-flex;
      flex-shrink: 0;
      line-height: 0;
    }
  `,
})
export class Icon {
  readonly name = input.required<IconName>();
  readonly size = input(20);
  readonly strokeWidth = input(2);

  protected readonly shape = computed<IconShape>(() => ICONS[this.name()]);
}
