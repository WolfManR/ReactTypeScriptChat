import { useQuery } from '@tanstack/react-query'
import React, { useReducer } from 'react'

type chat = {
  id: string
  name: string
  lastMessage: string
}

type Props = {
  setChatId: (value: string) => void
}

const ChatGroups = ({setChatId} : Props) => {
  const { isLoading, data } = useQuery({
    queryKey: ['chats'],
    queryFn: () =>
      fetch(`http://localhost:5271/groups`, {
        method: 'GET',
        credentials: 'include',
        mode: 'cors',
      }).then((response) => response.json() as Promise<Array<chat>>),
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
