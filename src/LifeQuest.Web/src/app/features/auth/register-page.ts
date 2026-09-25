import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '../../core/auth/auth.store';
import { fieldErrors, firstErrorMessage } from '../../core/http/api-error';
import { Button } from '../../ui/button';
import { AuthLayout } from './auth-layout';
import { APP_PATHS } from '../../core/routing/app-paths';

const currentYear = new Date().getFullYear();

function strongPassword(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value ?? '';
  return /[A-Za-zÇĞİÖŞÜçğıöşü]/.test(value) && /\d/.test(value) ? null : { weak: true };
}

@Component({
  selector: 'lq-register-page',
  imports: [ReactiveFormsModule, RouterLink, Button, AuthLayout],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <lq-auth-layout heading="Maceraya katıl" lead="Ekran başında değil, gerçek hayatta ilerlediğin bir oyun.">
      <form class="stack" [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <div class="field">
          <label for="displayName">Sana nasıl hitap edelim?</label>
          <input id="displayName" class="input" formControlName="displayName" autocomplete="given-name" [attr.aria-invalid]="invalid('displayName')" />
          @if (invalid('displayName')) {
            <span class="field__error">{{ serverErrors()['displayName'] ?? 'En az 2 karakter olmalı.' }}</span>
          }
        </div>
        <div class="field">
          <label for="email">E-posta</label>
          <input id="email" class="input" type="email" formControlName="email" autocomplete="email" inputmode="email" [attr.aria-invalid]="invalid('email')" />
          @if (invalid('email')) {
            <span class="field__error">{{ serverErrors()['email'] ?? 'Geçerli bir e-posta gir.' }}</span>
          }
        </div>
        <div class="field">
          <label for="password">Şifre</label>
          <input id="password" class="input" type="password" formControlName="password" autocomplete="new-password" [attr.aria-invalid]="invalid('password')" />
          @if (invalid('password')) {
            <span class="field__error">{{ serverErrors()['password'] ?? 'En az 8 karakter; harf ve rakam içermeli.' }}</span>
          } @else {
            <span class="field__hint">En az 8 karakter; harf ve rakam içermeli.</span>
          }
        </div>
        <div class="field">
          <label for="birthYear">Doğum yılın</label>
          <input id="birthYear" class="input" type="number" inputmode="numeric" formControlName="birthYear" [attr.aria-invalid]="invalid('birthYear')" />
          <span class="field__hint">LifeQuest 18 yaş ve üzeri içindir. Yalnızca doğum yılını saklıyoruz.</span>
        </div>
        @if (error()) {
          <p class="field__error" role="alert">{{ error() }}</p>
        }
        <button lq-button type="submit" [block]="true" [loading]="busy()" [disabled]="busy()">Hesap oluştur</button>
      </form>
      <p footer class="switch">Zaten hesabın var mı? <a [routerLink]="paths.login">Giriş yap</a></p>
    </lq-auth-layout>
  `,
  styles: `.switch { text-align: center; color: var(--ink-2); }`,
})
export class RegisterPage {
  protected readonly paths = APP_PATHS;
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly serverErrors = signal<Partial<Record<string, string>>>({});
  protected readonly form = inject(FormBuilder).nonNullable.group({
    displayName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), strongPassword]],
    birthYear: [currentYear - 25, [Validators.required, Validators.min(1900), Validators.max(currentYear)]],
  });

  protected invalid(name: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[name];
    return (control.invalid && control.touched) || !!this.serverErrors()[name];
  }

  protected submit(): void {
    this.serverErrors.set({});
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigate([APP_PATHS.onboarding]),
      error: (err: unknown) => {
        const fields = fieldErrors(err);
        this.serverErrors.set(fields);
        if (Object.keys(fields).length === 0) this.error.set(firstErrorMessage(err));
        this.busy.set(false);
      },
    });
  }
}
