export const isSigned = (controller: AbortController): Promise<boolean> =>
  fetch('http://localhost:5271/auth/signed-in', {
    mode: 'cors',
    credentials: 'include',
    signal: controller.signal,
  })
    .then((r) => {
      if (r.status === 401) {
        return false
      }
      if (r.status >= 200 && r.status <= 300) {
        return true
      }

      return false
    })
    .catch((e) => false)

export const signIn = (nick: string) =>
  fetch(`http://localhost:5271/auth/signin?nick=${nick}`, {
    method: 'POST',
    credentials: 'include',
    mode: 'cors',
  })
