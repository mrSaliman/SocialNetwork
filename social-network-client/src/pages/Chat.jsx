import React, { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { addFriendToChat, getChat, getCurrentUserId, getMessages, getNonChatFriends, getUser, toQueryString, WS_BASE_URL } from "../utils/apiHelper";
import CryptoJS from "crypto-js";
import Button from "../components/UI/buttons/Button";
import { useAuth } from "../hooks/AuthContext";
import Modal from "../components/UI/modal/Modal";

const Chat = () => {
  const id = useParams();
  const navigator = useNavigate();
  const { logout } = useAuth();
  const wsRef = useRef(null);

  const [messages, setMessages] = useState([]);
  const [currentUserId, setCurrentUserId] = useState(0);
  const [currentUser, setCurrentUser] = useState(null);

  const [friendsModal, setFriendsModal] = useState(false);
  const [friends, setFriends] = useState([]);

  const [chatName, setChatName] = useState();
  const [message, setMessage] = useState('');

  const messagesEndRef = useRef(null);

  useEffect(() => {
    if (messagesEndRef.current) {
      messagesEndRef.current.scrollIntoView({ behavior: "smooth" });
    }
  }, [messages]); 

  useEffect(() => {
    const fetchData = async () => {
      const resultUser = await getUser(null);
      setCurrentUser(resultUser);
      setCurrentUserId(getCurrentUserId());
      const resultMessages = await getMessages(id.id);
      resultMessages.sort((a, b) => new Date(a.Timestamp) - new Date(b.Timestamp));
      setMessages(resultMessages.map(m => {
        const hash = CryptoJS.MD5(m.Sender.Username).toString(CryptoJS.enc.Hex);
        return {
          id: m.Id,
          senderId: m.Sender.Id,
          senderName: m.Sender.Username.length > 36 ? m.Sender.Username.substring(0, 36) + '...' : m.Sender.Username,
          content: m.Content,
          timestamp: new Date(m.Timestamp).toUTCString(),
          avatar: `https://www.gravatar.com/avatar/${hash}?s=200&d=identicon`
        }
      }));

      const resultChat = await getChat(id.id);
      setChatName(resultChat.Name.length > 65 ? resultChat.Name.substring(0, 65) + '...' : resultChat.Name);
    };

    setCurrentUserId(getCurrentUserId());
    const token = localStorage.getItem("token");
    const wsUrl = `${WS_BASE_URL}?Type=Join&GroupId=${id.id}&Token=${token}`;
    wsRef.current = new WebSocket(wsUrl);

    wsRef.current.onopen = () => {
      console.log("WebSocket соединение установлено");
      wsRef.current.send(JSON.stringify({ Type: "Join", Content: "" }));
    };

    wsRef.current.onmessage = (event) => {
      const resultModel = JSON.parse(event.data);
      console.log(resultModel);
      const hash = CryptoJS.MD5(resultModel.Sender.Username).toString(CryptoJS.enc.Hex);
      var model = {
        id: resultModel.Id,
        senderId: resultModel.Sender.Id,
        senderName: resultModel.Sender.Username.length > 36 ? resultModel.Sender.Username.substring(0, 36) + '...' : resultModel.Sender.Username,
        content: resultModel.Content,
        timestamp: resultModel.Timestamp,
        avatar: `https://www.gravatar.com/avatar/${hash}?s=200&d=identicon`
      }
      setMessages((m) => [...m, model]);
    };

    wsRef.current.onerror = (error) => {
      console.error("WebSocket ошибка:", error);
    };

    wsRef.current.onclose = () => {
      console.log("WebSocket соединение закрыто");
    };

    fetchData();
  }, [id])

  async function openFriendModal() {
    setFriendsModal(true);
    const resultFriends = await getNonChatFriends(id.id);
    setFriends(resultFriends.map(user => {
      const hash = CryptoJS.MD5(user.Username).toString(CryptoJS.enc.Hex);
      return {
        id: user.Id,
        name: user.Username.length > 44 ? user.Username.substring(0, 44) + '...' : user.Username, 
        avatar: `https://www.gravatar.com/avatar/${hash}?s=200&d=identicon`
      }
    }));
  }

  const handleAddToChat = async (friendId) => {
    const model = {FriendId: friendId, GroupId: id.id};
    if (await addFriendToChat(model)) {
      setFriends(friends.filter(e => e.id !== friendId));
    }
  };

  const handleSendMessage = async () => {
    if (wsRef.current && wsRef.current.readyState === WebSocket.OPEN && message !== "") {
      const hash = CryptoJS.MD5(currentUser.Username).toString(CryptoJS.enc.Hex);
      var model = {
        id: messages[messages.length-1] ? messages[messages.length-1].id+1 : 1,
        senderId: currentUserId,
        senderName:  currentUser.Username.length > 36 ? currentUser.Username.substring(0, 36) + '...' : currentUser.Username,
        content: message,
        timestamp: new Date().toUTCString(),
        avatar: `https://www.gravatar.com/avatar/${hash}?s=200&d=identicon`
      }
      const payload = JSON.stringify({ Type: "Message", Content: message });
      wsRef.current.send(payload);
      setMessages((m) => [...m, model]);
      setMessage("");
    }
  };

  const handleLogout = () => { 
    wsRef.current.close();
    logout();
  }

  const handleKeyDown = async (e) => {
    if (e.key === 'Enter') {
      await handleSendMessage();
    }
  };

  return (
    <div className="flex flex-col h-screen bg-gray-100">
      <header className="bg-blue-500 text-white flex justify-between items-center">
        <div className="text-lg font-bold pl-3">
          <Button onClick={() => {wsRef.current.close(); navigator('/');}}>SD</Button>
        </div>

        <nav className="flex gap-4">
          <a onClick={() => {wsRef.current.close(); navigator('/chats')}} className="hover:underline">
              Чаты
          </a>
        </nav>
        <h1 className="flex gap-4 text-xl text-center font-semibold mr-10">{chatName}</h1>
        <Button onClick={handleLogout}>Выйти</Button>
      </header>

      <div className="flex-grow overflow-y-auto p-6 space-y-4">
      {messages.map((msg) => (
        <div
          key={msg.id}
          className={`flex items-start space-x-4 ${
            msg.senderId == currentUserId ? "justify-end" : "justify-start"
          }`}
        >
          {msg.senderId != currentUserId && (
            <button><img onClick={() => navigator(`/blog/${msg.senderId}`)} src={msg.avatar} alt={msg.senderName} className="w-10 h-10 rounded-full"/></button>
          )}
          
          <div
            className={`max-w-xs p-4 rounded-lg shadow ${
              msg.senderId == currentUserId
                ? "bg-blue-500 text-white"
                : "bg-gray-200 text-gray-900"
            }`}
          >
            {msg.senderId != currentUserId && (
              <p className="text-sm font-semibold mb-1">{msg.senderName}</p>
            )}
            <div
              className="message-content"
              dangerouslySetInnerHTML={{ __html: msg.content }}
            ></div>
            <span className="block text-xs text-black-500 mt-1">{msg.timestamp}</span>
          </div>
          {msg.senderId == currentUserId && (
            <button><img onClick={() => navigator(`/`)} src={msg.avatar} alt={msg.senderName} className="w-10 h-10 rounded-full"/></button>
          )}
          
        </div>
      ))}
      <div ref={messagesEndRef}/>
      </div> 
      <div className="p-4 bg-white shadow-lg">
        <div className="flex items-center space-x-4">
          <button onClick={() => openFriendModal()} className="px-4 py-2 bg-blue-600 text-white rounded-lg shadow hover:bg-blue-700 transition">
            <i className="fa-solid fa-user-plus"></i>
          </button>
          <input
            type="text"
            onKeyDown={handleKeyDown}
            value={message}
            onChange={e => setMessage(e.target.value)}
            placeholder="Напишите сообщение..."
            className="flex-grow p-3 border border-gray-300 rounded-lg shadow focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <button
            onClick={handleSendMessage} 
            className="px-4 py-2 bg-blue-600 text-white rounded-lg shadow hover:bg-blue-700 transition">
            <i className="fa-solid fa-share"></i>
          </button>
          
        </div>
      </div>
      <Modal isOpen={friendsModal} onClose={() => setFriendsModal(false)}>
        <div className="max-w-2xl p-6 bg-white rounded-lg shadow-lg" style={{width: 800}}>
          <h2 className="text-xl text-center font-bold mb-4">Список друзей</h2>
          <div className="max-h-96 overflow-y-auto">
          <ul className="space-y-4">
            {friends.map((user) => (
              <li key={user.id} className="flex items-center justify-between">
                <div className="flex items-center">
                  <img
                    src={user.avatar}
                    alt={user.name}
                    className="w-12 h-12 rounded-full mr-4"
                  />
                  <span className="text-lg font-medium"><Button variant="secondary" onClick={() => {wsRef.current.close(); navigator(`/blog/${user.id}`);}}>{user.name}</Button></span>
                </div>
                <div className="ml-auto flex space-x-2">
                  <button
                    onClick={() => handleAddToChat(user.id)}
                    className="h-12 w-12 px-3 py-1 bg-green-500 text-white rounded-md shadow hover:bg-green-600">
                    <i className="fa-solid fa-user-plus"></i>
                  </button>
                </div>
              </li>
            ))}
          </ul>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default Chat;