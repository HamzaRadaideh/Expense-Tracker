document.addEventListener("DOMContentLoaded", () => {
    const app = document.getElementById("app");
    const { friendId, currentUserId } = window.chatConfig;

    // Create the main container
    const chatContainer = document.createElement("div");
    chatContainer.style.cssText = `
        display: flex;
        flex-direction: column;
        height: 80vh;
        max-width: 600px;
        margin: auto;
        background-color: #1a1a1a;
        border-radius: 8px;
        padding: 10px;
    `;
    app.appendChild(chatContainer);

    // Create messages list
    const messagesList = document.createElement("ul");
    messagesList.style.cssText = `
        flex: 1;
        overflow-y: auto;
        list-style: none;
        padding: 10px;
        margin: 0;
        background-color: #101010;
        border-radius: 8px;
    `;
    chatContainer.appendChild(messagesList);

    // Create input area container
    const chatInput = document.createElement("div");
    chatInput.style.cssText = `
        display: flex;
        align-items: center;
        gap: 8px;
        margin-top: 10px;
    `;
    chatContainer.appendChild(chatInput);

    // Create message input
    const messageInput = document.createElement("input");
    messageInput.type = "text";
    messageInput.placeholder = "Type a message";
    messageInput.style.cssText = `
        flex: 1;
        padding: 10px;
        border-radius: 20px;
        border: none;
        outline: none;
        background-color: #333;
        color: #fff;
    `;
    chatInput.appendChild(messageInput);

    // Create send button
    const sendButton = document.createElement("button");
    sendButton.textContent = "Send";
    sendButton.style.cssText = `
        padding: 10px 20px;
        border-radius: 20px;
        border: none;
        background-color: #65da41;
        color: #fff;
        font-weight: bold;
        cursor: pointer;
    `;
    chatInput.appendChild(sendButton);

    // SignalR setup
    const connection = new signalR.HubConnectionBuilder().withUrl("/chathub").build();
    let isConnected = false;

    // Function to display messages based on sender and receiver IDs
    function displayMessage(senderUserId, senderFullName, message) {
        const li = document.createElement("li");

        // Determine if the message is sent by the current user or the friend
        const isCurrentUser = senderUserId === currentUserId;
        li.style.cssText = `
            display: block;
            padding: 10px;
            margin: 5px 0;
            border-radius: 10px;
            max-width: 70%;
            word-wrap: break-word;
            background-color: ${isCurrentUser ? "#65da41" : "#1a222b"};
            color: #fff;
            align-self: ${isCurrentUser ? "flex-end" : "flex-start"};
        `;

        const nameSpan = document.createElement("span");
        nameSpan.textContent = isCurrentUser ? "You: " : `${senderFullName}: `;
        nameSpan.style.cssText = `
            display: block;
            color: #adb5bd;
            font-size: 0.85em;
            margin-bottom: 3px;
        `;

        const messageSpan = document.createElement("span");
        messageSpan.textContent = message;
        messageSpan.style.cssText = `
            font-size: 1em;
            color: #fff;
        `;

        li.appendChild(nameSpan);
        li.appendChild(messageSpan);
        messagesList.appendChild(li);

        // Scroll to the latest message
        messagesList.scrollTop = messagesList.scrollHeight;
    }

    // Event listener for incoming messages
    connection.on("ReceiveMessage", (senderUserId, senderFullName, message) => {
        displayMessage(senderUserId, senderFullName, message);
    });

    // Error message event listener
    connection.on("ErrorMessage", (error) => {
        alert(error);
        console.error("Error message received:", error);
    });

    // Function to display a toast notification
    function displayToastNotification(title, message) {
        const notification = document.createElement("div");
        notification.className = "toast-notification";
        notification.innerHTML = `<strong>${title}</strong><p>${message}</p>`;

        // Apply toast notification styles
        notification.style.cssText = `
            position: fixed;
            bottom: 20px;
            right: 20px;
            background-color: #333;
            color: #fff;
            padding: 10px 15px;
            border-radius: 5px;
            box-shadow: 0 0 10px rgba(0, 0, 0, 0.5);
            z-index: 1000;
            opacity: 1;
            transition: opacity 0.5s;
        `;

        document.body.appendChild(notification);
        setTimeout(() => notification.remove(), 5000); // Auto-dismiss after 5 seconds
    }

    // Real-time notification for new messages
    connection.on("ShowNotification", (title, message) => {
        displayToastNotification(title, message);
    });

    // Send message function
    async function sendMessageToUser() {
        const message = messageInput.value.trim();

        if (!message) {
            alert("Message cannot be empty!");
            return;
        }

        if (!isConnected) {
            alert("Connection is not ready. Please wait.");
            return;
        }

        await connection.invoke("SendMessageToUser", friendId, message);
        messageInput.value = "";
    }

    sendButton.addEventListener("click", sendMessageToUser);

    // Send message on Enter key
    messageInput.addEventListener("keypress", (e) => {
        if (e.key === "Enter") {
            sendMessageToUser();
        }
    });

    // Set the active chat when the user opens a chat with a friend
    function setActiveChat() {
        connection.invoke("SetActiveChat", friendId).catch(err => console.error(err.toString()));
    }

    // Clear the active chat when the user closes the chat or navigates away
    function clearActiveChat() {
        connection.invoke("ClearActiveChat").catch(err => console.error(err.toString()));
    }

    // Call setActiveChat when the chat opens, and clearActiveChat when it closes
    setActiveChat(); // Set active chat initially
    window.addEventListener("beforeunload", clearActiveChat); // Clear on page unload

    // Load chat history and establish connection
    connection.start()
        .then(async () => {
            isConnected = true;
            await loadChatHistory();
        })
        .catch(err => console.error("Error while starting SignalR connection:", err));

    // Function to load and display chat history
    async function loadChatHistory() {
        const messages = await connection.invoke("LoadChatHistory", friendId);
        messages.forEach(msg => {
            displayMessage(msg.SenderId, msg.SenderFullName, msg.Content);
        });
    }
});
