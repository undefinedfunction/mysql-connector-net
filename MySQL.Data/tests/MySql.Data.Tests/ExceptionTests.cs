// Copyright (c) 2013, 2021, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using NUnit.Framework;
using System.Data;
using System.Threading;

namespace MySql.Data.MySqlClient.Tests
{
  public class ExceptionTests : TestBase
  {
    [Test]
    public void Timeout()
    {
      ExecuteSQL("CREATE TABLE Test (id INT NOT NULL, name VARCHAR(100))");
      for (int i = 1; i < 10; i++)
        ExecuteSQL("INSERT INTO Test VALUES (" + i + ", 'This is a long text string that I am inserting')");

      // we create a new connection so our base one is not closed
      var connection = GetConnection(false);
      KillConnection(connection);
      MySqlCommand cmd = new MySqlCommand("SELECT * FROM Test", connection);    
       
      Exception ex = Assert.Throws<InvalidOperationException>(() =>  cmd.ExecuteReader());
      Assert.AreEqual("Connection must be valid and open.", ex.Message);     
      Assert.AreEqual(ConnectionState.Closed, connection.State);
      connection.Close();
      
    }
    /// <summary>
    /// Bug #27436 Add the MySqlException.Number property value to the Exception.Data Dictionary  
    /// </summary>
    [Test]
    public void ErrorData()
    {
      MySqlCommand cmd = new MySqlCommand("SELEDT 1", Connection);
      try
      {
        cmd.ExecuteNonQuery();
      }
      catch (Exception ex)
      {
        Assert.AreEqual(1064, ex.Data["Server Error Code"]);
      }
    }

    /// <summary>
    /// WL-14393 Improve timeout error messages
    /// </summary>
    [Test]
    public void TimeoutErrorMessages()
    {
      if (Version < new Version("8.0.24")) return;

      var builder = new MySqlConnectionStringBuilder(ConnectionSettings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.AllowPublicKeyRetrieval = true;
      builder.Database = "";
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SET SESSION wait_timeout=4;", connection);
        command.ExecuteNonQuery();
        Thread.Sleep(6000);
        command = new MySqlCommand("SELECT CONNECTION_ID();", connection);
        var ex = Assert.Throws<MySqlException>(() => command.ExecuteScalar());
        Assert.AreEqual((int)MySqlErrorCode.ErrorClientInteractionTimeout, ex.Number);
      }
    }

  }
}
