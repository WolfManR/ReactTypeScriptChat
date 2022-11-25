export type chat = {
  id: string
  name: string
  lastMessage: string
}

const requestConfig = (method: 'GET' | 'POST'): RequestInit => ({
  method: method,
  credentials: 'include',
  mode: 'cors',
})

export const getChats = () =>
  fetch(`http://localhost:5271/groups`, requestConfig('GET')).then(
    (r) => r.json() as Promise<Array<chat>>,
  )

export type message = {
  userName: string
  message: string
}

export type messagesResponse = {
  code: number
  message: string
  isFailure: boolean
  data: Array<message>
}

export const getMessages = (id: string) =>
  fetch(
    `http://localhost:5271/messages?chatGroupId=${id}`,
    requestConfig('GET'),
  ).then((r) => r.json() as Promise<messagesResponse>)
