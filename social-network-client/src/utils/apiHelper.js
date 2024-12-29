export const API_BASE_URL = 'http://192.168.0.107:8080';
export const WS_BASE_URL = 'ws://192.168.0.107:8080';

export function toQueryString(params) {
  return Object.entries(params)
    .flatMap(([key, value]) => {
      if (Array.isArray(value)) {
        return value.map(subValue => `${encodeURIComponent(key)}=${encodeURIComponent(subValue)}`);
      }
      if (typeof value === 'object' && value !== null) {
        return Object.entries(value)
          .map(([subKey, subValue]) => `${encodeURIComponent(key + '.' + subKey)}=${encodeURIComponent(subValue)}`);
      }
      return `${encodeURIComponent(key)}=${encodeURIComponent(value)}`;
    })
    .join('&');
}

function parseJwt(token) {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => `%${('00' + c.charCodeAt(0).toString(16)).slice(-2)}`)
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch (e) {
    console.error('Invalid token', e);
    return null;
  }
}

export const getCurrentUserId = () => {
  const token = localStorage.getItem('token');
  return parseJwt(token).id;
};

export const getBlogs = async (id) => {
  const token = localStorage.getItem('token');
  if (!id) {
    id = parseJwt(token).id;
  }
  const response = await fetch(`${API_BASE_URL}/blogs/${id}`, {
    method: 'GET',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};

export const getUser = async (id) => {
  const token = localStorage.getItem('token');
  if (!id) {
    id = parseJwt(token).id;
  }
  const response = await fetch(`${API_BASE_URL}/user/${id}`, {
    method: 'GET',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};

export const addBlog = async (model) => {
  const token = localStorage.getItem('token');
  if (!model.AuthorId) {
    model.AuthorId = parseJwt(token).id;
  }
  const response = await fetch(`${API_BASE_URL}/create-blog`, {
    method: 'POST',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(model)
    });
  return response.status;
};

export const getNonFriends = async (id) => {
  const token = localStorage.getItem('token');
  if (!id) {
    id = parseJwt(token).id;
  }
  const response = await fetch(`${API_BASE_URL}/not-friends/${id}`, {
    method: 'GET',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};

export const getBlockUsers = async (id) => {
  const token = localStorage.getItem('token');
  if (!id) {
    id = parseJwt(token).id;
  }
  const response = await fetch(`${API_BASE_URL}/blocked/${id}`, {
    method: 'GET',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};

export const getFriends = async (id) => {
  const token = localStorage.getItem('token');
  if (!id) {
    id = parseJwt(token).id;
  }
  const response = await fetch(`${API_BASE_URL}/friends/${id}`, {
    method: 'GET',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};

export const getChats = async () => {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE_URL}/groups`, {
    method: 'GET',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};

export const getChat = async (id) => {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE_URL}/group/${id}`, {
    method: 'GET',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};

export const getMessages = async (id) => {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE_URL}/messages/${id}`, {
    method: 'GET',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};

export const getNonChatFriends = async (id) => {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE_URL}/group-friends/${id}`, {
    method: 'GET',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};

export const addFriend = async (model) => {
  const token = localStorage.getItem('token');
  if (!model.UserId) {
    model.UserId = parseJwt(token).id;
  }
  const response = await fetch(`${API_BASE_URL}/add-friend`, {
    method: 'POST',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(model)
    });
  return response.status;
};

export const blockFriend = async (model) => {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE_URL}/block-user`, {
    method: 'POST',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(model)
    });
  return response.status;
};

export const unBlockFriend = async (model) => {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE_URL}/unblock-user`, {
    method: 'POST',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(model)
    });
  return response.status;
};

export const deleteFriend = async (model) => {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE_URL}/remove-friend`, {
    method: 'POST',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(model)
    });
  return response.status;
};

export const addChat = async (model) => {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE_URL}/create-group`, {
    method: 'POST',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(model)
    });
  return await response.json();
};

export const addFriendToChat = async (model) => {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE_URL}/join-group`, {
    method: 'POST',
    headers: {
      "ngrok-skip-browser-warning": "69420",
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(model)
    });
  return response.status;
};