import React, { useEffect, useState } from "react";
import CryptoJS from "crypto-js";
import Modal from '../components/UI/modal/Modal'
import { getBlogs, getUser, addBlog, getNonFriends, addFriend, blockFriend, getBlockUsers, unBlockFriend, deleteFriend, getFriends } from "../utils/apiHelper";
import { useNavigate, useParams } from "react-router-dom";
import Button from "../components/UI/buttons/Button";
import DOMPurify from "dompurify";

const Home = () => {
  const { id } = useParams();
  const navigate = useNavigate();

  const [user, setUser] = useState({name: '', avatar: ''});
  
  const [addFriendsModal, setAddFriendsModal] = useState(false);
  const [nonFriends, setNonFriends] = useState([]);

  const [friendsModal, setFriendsModal] = useState(false);
  const [friends, setFriends] = useState([]);

  const [blockModal, setBlockModal] = useState(false);
  const [blockUsers, setBlockUsers] = useState([]);

  const [addBlogModal, setAddBlogModal] = useState(false);
  const [blogText, setBlogText] = useState('');
  const [blogs, setBlogs] = useState([]);

  const handleAddFriend = async (id) => {
    const model = {FriendId: id};
    if (await addFriend(model)){
      setNonFriends(nonFriends.filter(e => e.id !== id));
    }
  };

  const handleBlockFriend = async (id, collectionName) => {
    const model = {FriendId: id};
    if (await blockFriend(model)) {
      if (collectionName == 'nonFriends')
        setNonFriends(nonFriends.filter(e => e.id !== id));
      else 
        setFriends(friends.filter(e => e.id !== id));
    }
  };

  const handleUnBlockUser = async (id) => {
    const model = {FriendId: id};
    if (await unBlockFriend(model)){
      setBlockUsers(blockUsers.filter(e => e.id !== id));
    }
  };

  const handleDeleteFriend = async (id) => {
    const model = {FriendId: id};
    if (await deleteFriend(model)){
      setFriends(friends.filter(e => e.id !== id));
    }
  };

  useEffect(() => {
    const fetchData = async () => {
      const resultBlogs = await getBlogs(id);
      setBlogs(resultBlogs.reverse());

      const resultUser = await getUser(id);
      const hash = CryptoJS.MD5(resultUser.Username).toString(CryptoJS.enc.Hex);
      setUser({
        name: resultUser.Username.length > 28 ? resultUser.Username.substring(0, 28) + '...' : resultUser.Username, 
        avatar: `https://www.gravatar.com/avatar/${hash}?s=200&d=identicon`,});
    };

    fetchData();
  }, [id])

  async function saveBlog() {
    var model = {
      AuthorId: null,
      Content: blogText
    };
    setBlogText('');
    setAddBlogModal(false);
    if (await addBlog(model)){
      const resultBlogs = await getBlogs(null);
      setBlogs(resultBlogs.reverse());
    }
  }

  async function openAddFrindsModal() {
    setAddFriendsModal(true);
    const resultNonFriends = await getNonFriends(null);
    setNonFriends(resultNonFriends.map(user => {
      const hash = CryptoJS.MD5(user.Username).toString(CryptoJS.enc.Hex);
      return {
        id: user.Id,
        name: user.Username.length > 44 ? user.Username.substring(0, 44) + '...' : user.Username, 
        avatar: `https://www.gravatar.com/avatar/${hash}?s=200&d=identicon`
      }
    }));
  }

  async function openBlockModal() {
    setBlockModal(true);
    const resultBlockUsers = await getBlockUsers(null);
    setBlockUsers(resultBlockUsers.map(user => {
      const hash = CryptoJS.MD5(user.Username).toString(CryptoJS.enc.Hex);
      return {
        id: user.Id,
        name: user.Username.length > 44 ? user.Username.substring(0, 44) + '...' : user.Username, 
        avatar: `https://www.gravatar.com/avatar/${hash}?s=200&d=identicon`
      }
    }));
  }

  async function openFriendModal() {
    setFriendsModal(true);
    const resultFriends = await getFriends(null);
    setFriends(resultFriends.map(user => {
      const hash = CryptoJS.MD5(user.Username).toString(CryptoJS.enc.Hex);
      return {
        id: user.Id,
        name: user.Username.length > 44 ? user.Username.substring(0, 44) + '...' : user.Username, 
        avatar: `https://www.gravatar.com/avatar/${hash}?s=200&d=identicon`
      }
    }));
  }

  return (
    <div className="min-h-screen bg-gray-100 p-6">
      <div className="max-w-4xl mx-auto bg-white rounded-lg shadow-md p-6">
        <div className="flex items-center gap-4">
          <img
            src={user.avatar}
            alt={user.name}
            className="w-20 h-20 rounded-full border-2 border-blue-500"
          />
          <div>
            <h1 className="text-2xl font-bold">{user.name}</h1>
          </div>
          {!id ? (
            <div>
            <button onClick={() => openAddFrindsModal()} className="px-4 py-2 bg-blue-500 text-white rounded-lg shadow hover:bg-blue-600 w-24 ml-5">
              <i className="fa-solid fa-user-plus"></i>
            </button>
            <button onClick={() => openFriendModal()} className="px-4 py-2 bg-blue-500 text-white rounded-lg shadow hover:bg-blue-600 w-24 ml-2">
              <i className="fa-solid fa-users"></i>
            </button>
            <button onClick={() => openBlockModal()} className="px-4 py-2 bg-red-500 text-white rounded-lg shadow hover:bg-red-600 w-24 ml-2">
              <i className="fa-solid fa-ban"></i>
            </button>
          </div>
          ) : <></>}
          
        </div>

        <div className="flex gap-4"/>

        <div className="mt-8">
          {!id ? (
            <h2 className="text-xl font-semibold border-b pb-2 mb-4">
              <button onClick={() => setAddBlogModal(true)} className="px-4 py-2 bg-blue-500 text-white rounded-lg shadow hover:bg-blue-600 w-24">
                <i className="fa-solid fa-circle-plus"></i>
              </button>
            </h2>
          ) : <></>}
          <ul className="space-y-6">
            {blogs.map((blog) => (
              <li
                key={blog.Id}
                className="p-6 bg-white rounded-xl shadow-md border border-gray-200 hover:shadow-lg transition flex justify-between items-center">
                <div>
                  <div
                    className="message-content"
                    dangerouslySetInnerHTML={{ __html: blog.Content }}
                  ></div>
                  <p className="text-sm text-gray-500 italic">
                    Опубликовано: {blog.Timestamp}
                  </p>
                </div>
              </li>
            ))}
          </ul>
        </div>
      </div>

      {!id ? (
        <>
          <Modal isOpen={addBlogModal} onClose={() => setAddBlogModal(false)}>
          <div className="max-w-2xl p-1 bg-white rounded-lg shadow-lg" style={{width: 800}}>
            <textarea
              value={blogText}
              onChange={e => setBlogText(e.target.value)}
              className="w-full p-2 border border-gray-300 rounded-md resize-none focus:outline-none focus:ring-2 focus:ring-blue-500"
              rows="15"
              placeholder="Введите текст поста..."
            />
            <button
              onClick={saveBlog}
              className="mt-2 px-4 py-2 bg-green-500 text-white rounded-md shadow hover:bg-green-600">
              <i className="fa-solid fa-circle-plus"></i>
            </button>
          </div>
          </Modal>

          <Modal isOpen={addFriendsModal} onClose={() => setAddFriendsModal(false)}>
            <div className="max-w-2xl p-6 bg-white rounded-lg shadow-lg" style={{width: 800}}>
              <h2 className="text-xl text-center font-bold mb-4">Список пользователей</h2>
              <div className="max-h-96 overflow-y-auto">
              <ul className="space-y-4">
                {nonFriends.map((user) => (
                  <li key={user.id} className="flex items-center justify-between">
                    <div className="flex items-center">
                      <img
                        src={user.avatar}
                        alt={user.name}
                        className="w-12 h-12 rounded-full mr-4"
                      />
                      <span className="text-lg font-medium">{user.name}</span>
                    </div>
                    <div className="ml-auto flex space-x-2">
                      <button
                        onClick={() => handleAddFriend(user.id)}
                        className="h-12 w-12 px-3 py-1 bg-green-500 text-white rounded-md shadow hover:bg-green-600"
                      >
                        <i className="fa-solid fa-circle-plus"></i>
                      </button>
                      <button
                        onClick={() => handleBlockFriend(user.id, 'nonFriends')}
                        className="h-12 w-12 px-3 py-1 bg-red-500 text-white rounded-md shadow hover:bg-red-600">
                          <i className="fa-solid fa-ban"></i>
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
              </div>
            </div>
          </Modal>

          <Modal isOpen={blockModal} onClose={() => setBlockModal(false)}>
            <div className="max-w-2xl p-6 bg-white rounded-lg shadow-lg" style={{width: 800}}>
              <h2 className="text-xl text-center font-bold mb-4">Список заблокированных пользователей</h2>
              <div className="max-h-96 overflow-y-auto">
              <ul className="space-y-4">
                {blockUsers.map((user) => (
                  <li key={user.id} className="flex items-center justify-between">
                    <div className="flex items-center">
                      <img
                        src={user.avatar}
                        alt={user.name}
                        className="w-12 h-12 rounded-full mr-4"
                      />
                      <span className="text-lg font-medium">{user.name}</span>
                    </div>
                    <div className="ml-auto flex space-x-2">
                      <button
                        onClick={() => handleUnBlockUser(user.id)}
                        className="h-12 w-12 px-3 py-1 bg-green-500 text-white rounded-md shadow hover:bg-green-600">
                          <i className="fa-solid fa-ban"></i>
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
              </div>
            </div>
          </Modal>

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
                      <span className="text-lg font-medium"><Button variant="secondary" onClick={() => navigate(`/blog/${user.id}`)}>{user.name}</Button></span>
                    </div>
                    <div className="ml-auto flex space-x-2">
                      <button
                        onClick={() => handleDeleteFriend(user.id)}
                        className="h-12 w-12 px-3 py-1 bg-orange-500 text-white rounded-md shadow hover:bg-orange-600">
                        <i className="fa-solid fa-user-minus"></i>
                      </button>
                      <button
                        onClick={() => handleBlockFriend(user.id, 'friends')}
                        className="h-12 w-12 px-3 py-1 bg-red-500 text-white rounded-md shadow hover:bg-red-600">
                          <i className="fa-solid fa-ban"></i>
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
              </div>
            </div>
          </Modal>
        </>
      ) : <></>}
    </div>
  );
};

export default Home;