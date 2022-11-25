import { useMutation, useQuery } from '@tanstack/react-query'
import React, { useRef } from 'react'
import { signIn } from '../api/auth-api'

const SignIn = () => {
  const mutation = useMutation({
    mutationFn: signIn,
  })

  const inputRef = useRef<HTMLInputElement>(null)

  return (
    <div>
      <form>
        <label>
          <p>Username</p>
          <input ref={inputRef} type="text" />
        </label>
        <div>
          <button
            type="submit"
            onClick={(e) => {
              e.preventDefault()
              mutation.mutate(inputRef.current?.value as string)
            }}
          >
            Sign In
          </button>
        </div>
      </form>
    </div>
  )
}

export default SignIn
