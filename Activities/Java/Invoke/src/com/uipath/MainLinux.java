package com.uipath;

import com.uipath.server.DotNetProcessRequest;
import com.uipath.server.Response;

import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.RandomAccessFile;
import java.net.StandardProtocolFamily;
import java.net.UnixDomainSocketAddress;
import java.nio.ByteBuffer;
import java.nio.channels.SocketChannel;
import java.nio.file.InvalidPathException;
import java.nio.file.Path;
import java.util.logging.Level;
import java.util.logging.Logger;

public class MainLinux {
    private static final Logger LOGGER = Logger.getLogger( Main.class.getName() );

    public static void main(String[] args) {
        if (args.length == 0) {
            return;
        }
        String pipeName = "CoreFxPipe_" + args[0];
        if (args[0] == null || args[0].length() == 0) {
            LOGGER.log(Level.SEVERE, "The name of the pipe was not supplied");
            return;
        }
        LOGGER.log(Level.INFO, "Started java invoker with named pipe " + pipeName);
        DotNetProcessRequest broker = new DotNetProcessRequest();
        SocketChannel clientPipe = null;
        while (true) {
            try {
                if (clientPipe == null) {
                    var pipeFile = Path.of(System.getProperty("java.io.tmpdir"), pipeName);
                    System.out.println(pipeFile.toString());
                    UnixDomainSocketAddress address = UnixDomainSocketAddress.of(Path.of(System.getProperty("java.io.tmpdir"), pipeName));
                    clientPipe = SocketChannel.open(StandardProtocolFamily.UNIX);
                    clientPipe.connect(address);
                }
            }
            catch (InvalidPathException|IOException e) {
                continue;
            }
            try {
                if (!clientPipe.isConnected()) {
                    continue;
                }
                ByteBuffer msg=ByteBuffer.allocate(10241024);
                int bytesRead = clientPipe.read(msg);
                if (bytesRead <= 0){
                    continue;
                }
                byte[] bytes = new byte[bytesRead];
                msg.flip();
                msg.get(bytes);
                String text = new String(bytes);
                if (text == null || text.length() == 0) {
                    continue;
                }
                LOGGER.log(Level.INFO, "Data was received");

                Response response = broker.processRequest(text);
                var returnBytes =(response.toJson().toString() + '\n').getBytes();
                ByteBuffer buffer= ByteBuffer.allocate(returnBytes.length);
                buffer.clear();
                buffer.put(returnBytes);
                buffer.flip();
                clientPipe.write(buffer);

                LOGGER.log(Level.INFO, "Method was invoked and the result was sent");

                if (response.shouldJavaStop()) {
                    break;
                }
            } catch (IOException e) {
                LOGGER.log(Level.SEVERE, "Data could not be read from the pipe");
                break;
            }
        }
        LOGGER.log(Level.INFO, "Java invoker is stopping");
    }
}
