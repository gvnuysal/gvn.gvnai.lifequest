import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '../../core/auth/auth.store';
import { firstErrorMessage } from '../../core/http/api-error';
import { Button } from '../../ui/button';
import { AuthLayout } from './auth-layout';
import { APP_PATHS, HOME_PATH } from '../../core/routing/app-paths';

@Component({
  selector: 'lq-login-page',
  imports: [ReactiveFormsModule, RouterLink, Button, AuthLayout],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <lq-auth-layout heading="Tekrar hoş geldin" lead="Bugün gerçek hayatta seni bekleyen küçük maceralar var.">
      <form class="stack" [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <div class="field">
          <label for="email">E-posta</label>
          <input id="email" class="input" type="email" formControlName="email" autocomplete="email" inputmode="email" />
        </div>
        <div class="field">
          <label for="password">Şifre</label>
          <input id="password" class="input" type="password" formControlName="password" autocomplete="current-password" />
        </div>
        @if (error()) {
          <p class="field__error" role="alert">{{ error() }}</p>
        }
        <button lq-button type="submit" [block]="true" [loading]="busy()" [disabled]="busy()">Giriş yap</button>
      </form>
      <p footer class="switch">Hesabın yok mu? <a [routerLink]="paths.register">Kayıt ol</a></p>
    </lq-auth-layout>
  `,
  styles: `.switch { text-align: center; color: var(--ink-2); }`,
})
export class LoginPage {
  protected readonly paths = APP_PATHS;
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  readonly returnUrl = input<string>();

  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('E-posta ve şifreni gir.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigateByUrl(this.safeReturnUrl()),
      error: (err: unknown) => {
        this.error.set(firstErrorMessage(err));
        this.busy.set(false);
      },
    });
  }

  /** Açık yönlendirmeye karşı yalnızca uygulama içi yollar kabul edilir. */
  private safeReturnUrl(): string {
    const url = this.returnUrl();
    return url && url.startsWith('/') && !url.startsWith('//') ? url : HOME_PATH;
  }
}
