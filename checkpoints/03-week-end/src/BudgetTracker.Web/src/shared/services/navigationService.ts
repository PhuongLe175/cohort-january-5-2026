// This service allows us to handle navigation in a way that can be used by non-React code
let navigate: ((to: string) => void) | null = null;

export const navigationService = {
  setNavigate: (navigateFunction: (to: string) => void) => {
    navigate = navigateFunction;
  },

  navigateTo: (path: string) => {
    if (navigate) {
      navigate(path);
    } else {
      console.warn('Navigation function not set. Falling back to window.location');
      window.location.href = path;
    }
  }
};