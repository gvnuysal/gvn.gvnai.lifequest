import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '../../core/auth/auth.store';
import { firstErrorMessage } from '../../core/http/api-error';
import { Button } from '../../ui/button';
import { AuthLayout } from './auth-layout';
import { APP_PATHS, HOME_PATH } from '../../core/routing/app-paths';
import { t } from '../../core/i18n/i18n';

@Component({
  selector: 'lq-login-page',
  imports: [ReactiveFormsModule, RouterLink, Button, AuthLayout],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <lq-auth-layout [heading]="t().auth.loginHeading" [lead]="t().auth.loginLead">
      <form class="stack" [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <div class="field">
          <label for="email">{{ t().auth.email }}</label>
          <input id="email" class="input" type="email" formControlName="email" autocomplete="email" inputmode="email" />
        </div>
        <div class="field">
          <label for="password">{{ t().auth.password }}</label>
          <input id="password" class="input" type="password" formControlName="password" autocomplete="current-password" />
        </div>
        @if (error()) {
          <p class="field__error" role="alert">{{ error() }}</p>
        }
        <button lq-button type="submit" [block]="true" [loading]="busy()" [disabled]="busy()">{{ t().auth.login }}</button>
      </form>
      <p footer class="switch">{{ t().auth.noAccount }} <a [routerLink]="paths.register" [queryParams]="returnUrl() ? { returnUrl: returnUrl() } : {}">{{ t().auth.signUp }}</a></p>
    </lq-auth-layout>
  `,
  styles: `.switch { text-align: center; color: var(--ink-2); }`,
})
export class LoginPage {
  protected readonly t = t;
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
      this.error.set(t().auth.enterCredentials);
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
