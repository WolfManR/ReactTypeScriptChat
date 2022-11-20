import { useMutation, useQuery } from '@tanstack/react-query'
import React, { useRef } from 'react'

const SignIn = () => {
  const mutation = useMutation({
    mutationFn: (nick: string) =>
      fetch(`http://localhost:5271/auth/signin?nick=${nick}`, {
        method: 'POST',
        credentials: 'include',
        mode: 'cors',
      }),
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
