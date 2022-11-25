import { useQuery } from '@tanstack/react-query'
import React, { useReducer } from 'react'
import { getMessages } from '../api/chats-api'

type Props = {
  id: string
}

const Messages = ({ id }: Props) => {
  const { status, data } = useQuery({
    queryKey: ['chats', 'messages', id],
    queryFn: () => getMessages(id),
    enabled: id.length > 0,
  })

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
