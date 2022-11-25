import React, { useEffect, useState } from 'react'
import { isSigned } from '../api/auth-api'
import SignIn from './sign-in'

type Props = {
  children: React.ReactNode
}

const SignInCheck = ({ children }: Props) => {
  // TODO: find a better way to handle auth state
  const [isAuthenticated, setAuthenticationState] = useState<Boolean>(false)

  useEffect(() => {
    if (isAuthenticated) return
    const controller = new AbortController()
    isSigned(controller).then(r=> setAuthenticationState(r))
    return () => {
      controller.abort()
    }
  }, [])
  
  return (isAuthenticated ? <div>{children}</div> : <SignIn />)
}

export default SignInCheck
