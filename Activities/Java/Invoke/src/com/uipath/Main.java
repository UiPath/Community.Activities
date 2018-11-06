package com.uipath;

import com.uipath.server.DotNetProcessRequest;
import com.uipath.server.Response;

import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.RandomAccessFile;
import java.util.logging.Level;
import java.util.logging.Logger;

public class Main {
    private static final Logger LOGGER = Logger.getLogger( Main.class.getName() );

    public static void main(String[] args) {
        if (args.length == 0) {
            return;
        }
        String pipeName = args[0];
        if (args[0] == null || args[0].length() == 0) {
            LOGGER.log(Level.SEVERE, "The name of the pipe was not supplied");
            return;
        }
        LOGGER.log(Level.INFO, "Started java invoker with named pipe " + pipeName);
        DotNetProcessRequest broker = new DotNetProcessRequest();
        RandomAccessFile clientPipe = null;
        while (true) {
            try {
                if (clientPipe == null) {
                    clientPipe = new RandomAccessFile("\\\\.\\pipe\\" + pipeName, "rws");
                }
            }
            catch (FileNotFoundException e) {
                continue;
            }
            try {
                if (clientPipe.length() == 0) {
                    continue;
                }
                String text = clientPipe.readLine();
                if (text == null || text.length() == 0) {
                    continue;
                }
                LOGGER.log(Level.INFO, "Data was received");

                Response response = broker.processRequest(text);
                clientPipe.write((response.toJson().toString() + '\n').getBytes());

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
