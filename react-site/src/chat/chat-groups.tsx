import { useQuery } from '@tanstack/react-query'
import React, { useReducer } from 'react'
import { chat, getChats } from '../api/chats-api'

type Props = {
  setChatId: (value: string) => void
}

const ChatGroups = ({ setChatId }: Props) => {
  const { isLoading, data } = useQuery({
    queryKey: ['chats'],
    queryFn: getChats,
  })

  const openChat = (chatInfo: chat) => {
    setChatId(chatInfo.id)
  }

  if (isLoading) return <div>Loading...</div>

  return (
    <div className="chat-groups">
      {data?.map((c) => (
        <button key={c.id} onClick={() => openChat(c)}>
          <p>{c.name}</p>
          <p>{c.lastMessage}</p>
        </button>
      ))}
    </div>
  )
}

export default ChatGroups
