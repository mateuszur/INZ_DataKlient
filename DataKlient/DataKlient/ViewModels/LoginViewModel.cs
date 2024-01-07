﻿using DataKlient.Models;
using DataKlient.Services;
using DataKlient.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DataKlient.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
       public Command LoginCommand { get; }
       

        private bool isCheckBoxChecked=false;
        private bool isPasswordEnabled=true;

        private Color serverStatusColor=Color.Red;
        private readonly Timer _timer;

       

        public LoginViewModel()
        {
            
            //if (CheckLocalSession())
            //{
            //    _ = Shell.Current.GoToAsync("//ItemsPage");
            //}
           
             _timer = new Timer (CheckServerAvailability, null, 0, 1500 );

        }

        public async Task<bool> OnLoginClickedAsync(string username, string password)
        {
            try
            {
                TcpClient client = new TcpClient("185.230.225.4", 3333);
                NetworkStream stream = client.GetStream();

                //Obsługa rządania
                byte[] dataUser = Encoding.ASCII.GetBytes("Login " + username + " " + password);
                stream.Write(dataUser, 0, dataUser.Length);
                await Task.Delay(1500);

                //Obsługa odp string respon = sessionDetails.SessionID + " " + sessionDetails.DataTimeEnd + " "+userDetails.Privileges + " " + userDetails.Login + " " + userDetails.ID;
                dataUser = new byte[256];

                int bytes = stream.Read(dataUser, 0, dataUser.Length);
                string responseData = Encoding.ASCII.GetString(dataUser, 0, bytes);
                string[] parts = responseData.Split(' ');


                if (parts[0] == "LoginSuccessful" && (parts[4] == "1" || parts[4]=="2"))
                {

                    _ = Shell.Current.GoToAsync("//ItemsPage");
                    
                    SessionLocalDetailsService _session= new SessionLocalDetailsService();
                    SessionLocalDetailsItem _newSession = new SessionLocalDetailsItem();

                    _newSession.sessionID = parts[1];
                    _newSession.endSesionDate= DateTime.Parse(parts[2]);
                    _newSession.privileges = parts[3];
                    _newSession.userLogin = parts[4];
                    _newSession.userID= parts[5];
                    _newSession.isValid = true;

                   await  _session.AddItem(_newSession);

                    client.Close();
                    return true;

                }
                else
                {
                    client.Close();
                    return false;
                }

            }
            catch (Exception ex)
            {
                return false;
                //Przestrzeń na zrobienie zapisu błedu w pliku z logami
                Console.WriteLine(ex.ToString() + "\n");

            }


            

        }


        public bool IsCheckBoxChecked
        {
            get { return isCheckBoxChecked; }
            set
            {
                if (isCheckBoxChecked != value)
                {
                    isCheckBoxChecked = value;
                    ShowHidePassword();
                    OnPropertyChanged(nameof(IsCheckBoxChecked));
                }
            }

        }


        public bool IsPasswordEnabled
        {
            get { return isPasswordEnabled; }
            set
            {
                if (isPasswordEnabled != value)
                {
                    isPasswordEnabled = value;
                    OnPropertyChanged(nameof(IsPasswordEnabled));
                }

            }
        }

        private void ShowHidePassword()
        {
            if (IsCheckBoxChecked)
            {

                IsPasswordEnabled = false;
            }
            else
            {
                IsPasswordEnabled = true;
            }

        }

        // Metody sprawdzająca dostępność serwera i ustawiająca odpowiedni kolor

        public Color ServerStatusColor
        {
            get { return serverStatusColor; }
            set
            {
                if (serverStatusColor != value)
                {
                    serverStatusColor = value;
                    CheckServerAvailability(DateTime.Now);

                    OnPropertyChanged(nameof(ServerStatusColor));
                }
            }
        }

        private void CheckServerAvailability(object state)
        {
            // Logika sprawdzająca dostępność serwera
           Task<bool> temp = CheckServerAsync();
            bool isServerAvailable= temp.Result;
            if (isServerAvailable)
            {
                ServerStatusColor = Color.Green;
            }
            else
            {
                ServerStatusColor = Color.Red;
            }
        }


        private async Task<bool> CheckServerAsync()
        {
            TcpClient _client = null;
            try
            {
             _client= new TcpClient("185.230.225.4", 3333);
               NetworkStream _stream = _client.GetStream();
                byte[] data = Encoding.ASCII.GetBytes("Ping");
               await _stream.WriteAsync(data, 0, data.Length);

                await Task.Delay(1750);
                data = new byte[256];

                int bytes = await _stream.ReadAsync(data, 0, data.Length);
                string responseData = Encoding.ASCII.GetString(data, 0, bytes);

                if (responseData == "Pong")
                { 
                    _client.Close();
                    return true;
                }else
                {
                _client.Close();
                return false;
                }

            }catch (Exception ex) {
                Console.WriteLine(ex.ToString());
               
                return false;}
            finally
            {
                _client?.Close();
             
            }
        }

        //Koniec metody sprawdzająca dostępność serwera i ustawiająca odpowiedni kolor


        //Metody sprawdzajace czy są loklane sesje- sesja na serwerze 24h
        private async Task<bool> CheckLocalSessionAsync()
        {
            try {
                SessionLocalDetailsService _session = new SessionLocalDetailsService();
                SessionLocalDetailsItem _activSession = new SessionLocalDetailsItem();

                _activSession = await _session.GetItems();
                if (_activSession != null)
                {
                    TcpClient client = new TcpClient("185.230.225.4", 3333);
                    NetworkStream stream = client.GetStream();

                    byte[] dataSessio = Encoding.ASCII.GetBytes("IsSessionValid " + _activSession.sessionID + " " + _activSession.userID);
                    stream.Write(dataSessio, 0, dataSessio.Length);
                    await Task.Delay(1000);


                    dataSessio = new byte[256];

                    int bytes = stream.Read(dataSessio, 0, dataSessio.Length);
                    string responseData = Encoding.ASCII.GetString(dataSessio, 0, bytes);


                    if (responseData == "Sesion is valid")
                    {
                        return true;
                    }else
                    {
                        return false;
                    }


                }
                else { return false; }

            }
            catch (Exception e)
            {
                return false;
            }
        }


        private bool CheckLocalSession()
        {
            // Logika sprawdzająca dostępność serwera
            Task<bool> temp = CheckLocalSessionAsync();
            bool isSessionValid = temp.Result;
            if (isSessionValid)
            {
                return true;
            }else
            { return false; }    
           
        }


    }
}
