export const validatePassword = (password) => {
  const minLength = 8;
  const hasUpperCase = /[A-Z]/.test(password);
  const hasDigit = /\d/.test(password);
  
  if (password.length < minLength) {
    return 'The password must contain at least 8 characters';
  }
  if (!hasUpperCase) {
    return 'The password must contain at least one capital letter';
  }
  if (!hasDigit) {
    return 'The password must contain at least one digit';
  }
  return '';
};

export const validateEmail = (email) => {
  if (!email.includes('@')) {
    return 'The email must contain the "@" character';
  }
  return '';
};

export const validateEmpty = (value, fieldName) => {
  if (!value) {
    return `The ${fieldName} could not be empty`;
  }
  return '';
};

export const validateNumber = (value, fieldName, min, max) => {
  if (value < min || value > max) {
    return `The ${fieldName} could be from ${min} to ${max}`;
  }
  return '';
};