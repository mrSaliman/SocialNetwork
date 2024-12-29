import React, { useEffect, useState } from "react";
import { addChat, getChats } from "../utils/apiHelper";
import CryptoJS from "crypto-js";
import { useNavigate } from "react-router-dom";
import Input from "../components/UI/inputs/Input";
import Modal from "../components/UI/modal/Modal";

const Chats = () => {
  const navigator = useNavigate();
  const [chats, setChats] = useState([]);

  const [newChatModal, setNewChatModal] = useState(false);
  const [chatName, setChatName] = useState('');

  useEffect(() => {
      const fetchData = async () => {
        const resultChats = await getChats();
        setChats(resultChats.map(chat => {
          const hash = CryptoJS.MD5(chat.Name).toString(CryptoJS.enc.Hex);
          return {
            id: chat.Id,
            name: chat.Name.length > 65 ? chat.Name.substring(0, 65) + '...' : chat.Name, 
            avatar: `https://www.gravatar.com/avatar/${hash}?s=200&d=identicon`
          }
        }));
      };
  
      fetchData();
    }, [])

  async function handleAddChat() {
    const model = {Name: chatName};
    const result = await addChat(model);
    console.log(result);
    navigator(`/chat/${result}`);
  }

  return (
    <div className="min-h-screen bg-gray-100 py-8">
      <div className="max-w-4xl mx-auto bg-white p-6 rounded-lg shadow-lg">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-bold text-gray-800">Список чатов</h1>
          <button
            onClick={() => setNewChatModal(true)}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg shadow hover:bg-blue-700 transition">
            <i className="fa-solid fa-plus"></i>
          </button>
        </div>
        <ul className="space-y-4">
          {chats.map((chat) => (
            <li
              onClick={() => navigator(`/chat/${chat.id}`)}
              key={chat.id}
              className="flex items-center p-4 bg-gray-50 rounded-lg shadow hover:bg-blue-50 cursor-pointer transition"
            >
              <img
                src={chat.avatar}
                alt={chat.name}
                className="w-14 h-14 rounded-full mr-4 border border-gray-300"
              />
              <h2 className="text-lg font-medium text-gray-800">{chat.name}</h2>
            </li>
          ))}
        </ul>
      </div>

      <Modal isOpen={newChatModal} onClose={() => setNewChatModal(false)}>
        <div className="max-w-2xl p-1 bg-white rounded-lg shadow-lg" style={{width: 800}}>
          <Input
            value={chatName}
            onChange={e => setChatName(e.target.value)}
            placeholder="Введите название чата..."
          />
          <button
            onClick={handleAddChat}
            className="mt-2 px-4 py-2 bg-green-500 text-white rounded-md shadow hover:bg-green-600">
            <i className="fa-solid fa-circle-plus"></i>
          </button>
        </div>
      </Modal>
    </div>
  );
};

export default Chats;