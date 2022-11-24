import React, { useState } from 'react';
import './index.css';
import ChatGroups from "./chat-groups";
import Messages from "./messages";
import Sender from "./sender";
import Profile from "./profile";

const Index = () => {
    const [currentChat, setCurrentChat] = useState<string>("");

    return (
        <div className="grid">
            <div className="left-side-bar">
                <ChatGroups setChatId={setCurrentChat}/>
            </div>
            <div className="middle-list">
                <Messages id={currentChat}/>
            </div>
            <div className="sender-section">
                <Sender/>
            </div>
            <div className="right-side-bar">
                <Profile/>
            </div>
        </div>
    );
};

export default Index;