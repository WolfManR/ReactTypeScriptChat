import React, { useEffect, useState } from 'react'
import SignIn from './sign-in'

type Props = {
  children: React.ReactNode
}

const SignInCheck = ({ children }: Props) => {
   // TODO: find a better way to handle auth state
  const [isAuthenticated, setAuthenticationState] = useState<Boolean>(false)

  useEffect(() => {
    if (isAuthenticated) return

    fetch('http://localhost:5271/auth/signed-in', {
      mode: 'cors',
      credentials: 'include',
    })
      .then((r) => {
        if (r.status === 401) {
          setAuthenticationState(false)
          return r
        }
        if (r.status >= 200 && r.status <= 300) {
          setAuthenticationState(true)
          return r
        }

        return false
      })
      .catch((e) => setAuthenticationState(false))
  }, [])

  if (!isAuthenticated) {
    return <SignIn />
  }

  return <div>{children}</div>
}

export default SignInCheck
