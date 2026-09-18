import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { noAuthGuard } from './no-auth.guard';

describe('noAuthGuard', () => {
  const createRouterMock = () => ({ createUrlTree: vi.fn((path: string[]) => ({ path }) as unknown as UrlTree) });

  const runGuard = (isLoggedIn: boolean) => {
    const routerMock = createRouterMock();
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { isLoggedIn: () => isLoggedIn } },
        { provide: Router, useValue: routerMock },
      ],
    });
    const result = TestBed.runInInjectionContext(() => noAuthGuard({} as any, {} as any));
    return { result, routerMock };
  };

  it('returns true when user is NOT logged in', () => {
    const { result } = runGuard(false);
    expect(result).toBe(true);
  });

  it('returns a UrlTree redirecting to /clients when already logged in', () => {
    const { result, routerMock } = runGuard(true);

    expect(result).not.toBe(true);
    expect(routerMock.createUrlTree).toHaveBeenCalledWith(['/clients']);
  });

  it('does not call createUrlTree when not logged in', () => {
    const { routerMock } = runGuard(false);
    expect(routerMock.createUrlTree).not.toHaveBeenCalled();
  });
});
