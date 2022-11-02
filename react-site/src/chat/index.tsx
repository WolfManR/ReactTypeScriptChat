import React from 'react';
import './index.css';
import ChatGroups from "./chat-groups";
import Messages from "./messages";
import Sender from "./sender";
import Profile from "./profile";

const Index = () => {
    return (
        <div className="grid">
            <ChatGroups/>
            <Messages/>
            <Sender/>
            <Profile/>
        </div>
    );
};

export default Index;