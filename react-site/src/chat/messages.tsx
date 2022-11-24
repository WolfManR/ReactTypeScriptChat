import { useQuery } from '@tanstack/react-query'
import React, { useReducer } from 'react'

type message = {
  userName: string
  message: string
}

type messagesResponse = {
  code: number
  message: string
  isFailure: boolean
  data: Array<message>
}

type Props = {
  id: string
}

const Messages = ({ id }: Props) => {
  const { status, data } = useQuery({
    queryKey: ['chats', 'messages', id],
    queryFn: () =>
      fetch(`http://localhost:5271/messages?chatGroupId=${id}`, {
        method: 'GET',
        credentials: 'include',
        mode: 'cors',
      }).then((r) => r.json() as Promise<messagesResponse>),
    enabled: id.length > 0,
  })
  console.log(id)
  if (status !== 'success') return <div className="messages">status</div>

  return (
    <div className="messages">
      {data.data.map((m, index) => (
        <div key={index}>
          <p>{m.userName}</p>
          <p>{m.message}</p>
        </div>
      ))}
    </div>
  )
}

export default Messages
