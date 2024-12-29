import React, { useEffect, useRef } from 'react';
import ReactDOM from 'react-dom';

const Modal = ({ isOpen, onClose, children, className = '' }) => {
  const modalRef = useRef(null);

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
    return () => {
      document.body.style.overflow = '';
    };
  }, [isOpen]);

  const handleOverlayClick = (e) => {
    if (modalRef.current && !modalRef.current.contains(e.target)) {
      onClose();
    }
  };

  if (!isOpen) return null;

  return ReactDOM.createPortal(
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-gray-800 bg-opacity-50"
      onClick={handleOverlayClick}>
      <div
        ref={modalRef}
        className={`relative bg-white rounded-lg shadow-lg p-2 overflow-hidden ${className}`}
        onClick={(e) => e.stopPropagation()}>
        <div className="text-sm text-gray-700">{children}</div>
      </div>
    </div>,
    document.body
  );
};

export default Modal;