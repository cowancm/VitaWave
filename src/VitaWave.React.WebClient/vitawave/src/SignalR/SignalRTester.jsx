import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

function SignalRTester() {
  const [connection, setConnection] = useState(null);
  const [response, setResponse] = useState('');

  useEffect(() => {
    const connect = async () => {
      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl('https://localhost:7187/moduleHub') // Updated URL to use https and port 7187
        .withAutomaticReconnect()
        .build();

      newConnection.on('ReceiveWebpageRequest', (message) => {
        console.log('Received:', message);
        setResponse(message);
      });

      try {
        await newConnection.start();
        console.log('Connected to SignalR hub');

        // Invoke the server method (if you want to *call* a method)
        await newConnection.invoke('ReceiveWebpageRequest', 'foobar');
      } catch (error) {
        console.error('Connection failed: ', error);
      }

      setConnection(newConnection);
    };

    connect();
  }, []);

  return (
    <div>
      <h1>SignalR Test</h1>
      <p>Response from server: {response}</p>
    </div>
  );
}

export default SignalRTester;
