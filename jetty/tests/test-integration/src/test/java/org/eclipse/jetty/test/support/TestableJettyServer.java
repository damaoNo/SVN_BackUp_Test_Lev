//
//  ========================================================================
//  Copyright (c) 1995-2015 Mort Bay Consulting Pty. Ltd.
//  ------------------------------------------------------------------------
//  All rights reserved. This program and the accompanying materials
//  are made available under the terms of the Eclipse Public License v1.0
//  and Apache License v2.0 which accompanies this distribution.
//
//      The Eclipse Public License is available at
//      http://www.eclipse.org/legal/epl-v10.html
//
//      The Apache License v2.0 is available at
//      http://www.opensource.org/licenses/apache2.0.php
//
//  You may elect to redistribute this code under either of these licenses.
//  ========================================================================
//

package org.eclipse.jetty.test.support;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.net.InetAddress;
import java.net.MalformedURLException;
import java.net.URI;
import java.net.URL;
import java.net.UnknownHostException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Properties;

import org.eclipse.jetty.http.HttpScheme;
import org.eclipse.jetty.server.Connector;
import org.eclipse.jetty.server.NetworkConnector;
import org.eclipse.jetty.server.Server;
import org.eclipse.jetty.xml.XmlConfiguration;
import org.junit.Assert;
import org.junit.Ignore;

/**
 * Allows for setting up a Jetty server for testing based on XML configuration files.
 */
@Ignore
public class TestableJettyServer
{
    private List<URL> _xmlConfigurations;
    private final Map<String,String> _properties = new HashMap<String, String>();
    private Server _server;
    private int _serverPort;
    private String _scheme = HttpScheme.HTTP.asString();

    /* Popular Directories */
    private File baseDir;
    private File testResourcesDir;

    public TestableJettyServer() throws IOException
    {
        _xmlConfigurations = new ArrayList<URL>();
        Properties properties = new Properties();

        /* Establish Popular Directories */
        String baseDirPath = System.getProperty("basedir");
        if (baseDirPath == null)
        {
            baseDirPath = System.getProperty("user.dir",".");
        }
        baseDir = new File(baseDirPath);
        properties.setProperty("test.basedir",baseDir.getAbsolutePath());

        testResourcesDir = new File(baseDirPath,"src/test/resources".replace('/',File.separatorChar));
        properties.setProperty("test.resourcesdir",testResourcesDir.getAbsolutePath());

        File testDocRoot = new File(testResourcesDir,"docroots");
        properties.setProperty("test.docroot.base",testDocRoot.getAbsolutePath());

        File targetDir = new File(baseDir,"target");
        properties.setProperty("test.targetdir",targetDir.getAbsolutePath());

        File webappsDir = new File(targetDir,"webapps");
        properties.setProperty("test.webapps",webappsDir.getAbsolutePath());

        // Write out configuration for use by ConfigurationManager.
        File testConfig = new File(targetDir,"testable-jetty-server-config.properties");
        FileOutputStream out = new FileOutputStream(testConfig);
        properties.store(out,"Generated by " + TestableJettyServer.class.getName());
        
        for (Object key:properties.keySet())
            _properties.put(String.valueOf(key),String.valueOf(properties.get(key)));
    }

    public void addConfiguration(URL xmlConfig)
    {
        _xmlConfigurations.add(xmlConfig);
    }

    public void addConfiguration(File xmlConfigFile) throws MalformedURLException
    {
        _xmlConfigurations.add(xmlConfigFile.toURI().toURL());
    }

    public void addConfiguration(String testConfigName) throws MalformedURLException
    {
        addConfiguration(new File(testResourcesDir,testConfigName));
    }

    public void setProperty(String key, String value)
    {
        _properties.put(key,value);
    }

    public void load() throws Exception
    {
        XmlConfiguration last = null;
        Object[] obj = new Object[this._xmlConfigurations.size()];

        // Configure everything
        for (int i = 0; i < this._xmlConfigurations.size(); i++)
        {
            URL configURL = this._xmlConfigurations.get(i);
            System.err.println("configuring: "+configURL);
            XmlConfiguration configuration = new XmlConfiguration(configURL);
            if (last != null)
            {
                configuration.getIdMap().putAll(last.getIdMap());
            }
            configuration.getProperties().putAll(_properties);
            obj[i] = configuration.configure();
            last = configuration;
        }

        // Test for Server Instance.
        Server foundServer = null;
        int serverCount = 0;
        for (int i = 0; i < this._xmlConfigurations.size(); i++)
        {
            if (obj[i] instanceof Server)
            {
                if (obj[i].equals(foundServer))
                {
                    // Identical server instance found
                    break;
                }
                foundServer = (Server)obj[i];
                serverCount++;
            }
        }

        if (serverCount <= 0)
        {
            throw new Exception("Load failed to configure a " + Server.class.getName());
        }

        Assert.assertEquals("Server load count",1,serverCount);

        this._server = foundServer;
        this._server.setStopTimeout(1000);
        
    }

    public String getScheme()
    {
        return _scheme;
    }

    public void setScheme(String scheme)
    {
        this._scheme = scheme;
    }

    public void start() throws Exception
    {
        Assert.assertNotNull("Server should not be null (failed load?)",_server);

        _server.start();

        // Find the active server port.
        this._serverPort = ((NetworkConnector)_server.getConnectors()[0]).getLocalPort();
        Assert.assertTrue("Server Port is between 1 and 65535. Actually <" + _serverPort + ">",(1 <= this._serverPort) && (this._serverPort <= 65535));
    }

    public int getServerPort()
    {
        return _serverPort;
    }

    public void stop() throws Exception
    {
        _server.stop();
    }

    public URI getServerURI() throws UnknownHostException
    {
        StringBuffer uri = new StringBuffer();
        uri.append(this._scheme).append("://");
        uri.append(InetAddress.getLocalHost().getHostAddress());
        uri.append(":").append(this._serverPort);
        return URI.create(uri.toString());
    }

    public Server getServer()
    {
        return _server;
    }
}
