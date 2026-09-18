import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  const createRouterMock = () => ({ createUrlTree: vi.fn((path: string[]) => ({ path }) as unknown as UrlTree) });

  const runGuard = (isLoggedIn: boolean) => {
    const routerMock = createRouterMock();
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { isLoggedIn: () => isLoggedIn } },
        { provide: Router, useValue: routerMock },
      ],
    });
    const result = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));
    return { result, routerMock };
  };

  it('returns true when user is logged in', () => {
    const { result } = runGuard(true);
    expect(result).toBe(true);
  });

  it('returns a UrlTree redirecting to /auth/login when not logged in', () => {
    const { result, routerMock } = runGuard(false);

    expect(result).not.toBe(true);
    expect(routerMock.createUrlTree).toHaveBeenCalledWith(['/auth/login']);
  });

  it('does not call createUrlTree when user is logged in', () => {
    const { routerMock } = runGuard(true);
    expect(routerMock.createUrlTree).not.toHaveBeenCalled();
  });
});
