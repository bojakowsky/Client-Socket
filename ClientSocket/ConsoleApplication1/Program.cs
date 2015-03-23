using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Serwer;
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
           
            ///TCP port k
            string ip_str; // zmienna IP - adres IP, zapisana w stringu
            Console.WriteLine("Podaj adres IP serwera"); //komunikat tekstowy w konsoli 
            ip_str = Console.ReadLine(); // zczytanie adresu ip do zmiennej string 

            IPAddress ip_address = IPAddress.Parse(ip_str); // parsowanie ip string do klasy IPAddress, wczytanie zmiennej

            Console.WriteLine("Podaj nr portu, na ktorym chcesz nawiazac polaczenie"); // komunikat tekstowy w konsoli
            int port_k; // zmienna int - numer portu k, sluzy do nawiazywania polaczenia przez klienta
            port_k = Convert.ToInt32(Console.ReadLine()); // wczytanie zmiennej, parsowanie do int

            TcpClient client = new TcpClient(); // inicjacja instancji klasy TcpKlient 
            try
            {
                client.Connect(ip_str, port_k);  // polaczenie sie za pomoca TCP na podanym IP i porcie k
            }
            catch (System.Net.Sockets.SocketException)
            {
                Console.WriteLine("Zrestartuj program, sprawdz polaczenie sieciowe lub poprawnosc danych serwera!");
                Console.ReadKey();
                return;
            }
            UdpClient clientUDP = new UdpClient(); // inicjacja klasy client UDP
            IPEndPoint host = new IPEndPoint(ip_address, port_k+1); //zdef. zdalnego hosta dla polaczenia UDP

            CommandHeader header_s; // zbudowany przez nas naglowek
            //wstepna nieznaczaca inicjalizacja danych
            //stworzona na potrzeby sytuacja nieprzewidzianych
            string command = "BUSY";
            string data = "";
            int id_i = 0;
            header_s.id_num = id_i;
            header_s.command_length = 3;
            header_s.port = ((IPEndPoint)client.Client.LocalEndPoint).Port; // port przy wysylaniu UDP jest ustawiony na domyslny lokalny port TCP
            header_s.data_length = 0;
            header_s.part_num = 0;
            header_s.last_part = 1;
            /*
            COMMAND_LENGTH|ID_NUM    |PORT	      |DATA_LENGTH |PART_NUM   |LAST_PART |COMMAND         |DATA
            1B            |4B        |4B	      |4B     	   |4B	       |1B        |COMMAND_LENGTHB |DATA_LENGTHB
            fixed	      |fixed     |fixed	      |fixed       |fixed      |fixed     |vary            |vary
            as bits value |bits value|bits value  |bits value  |bits value |T/F       |as string       |string
            HEADER PART                                                    		  | command+data part
            18B									  | command_length+data_length bytes


            COMMAND_LENGTH - dl polecenia
            COMMAND - polecenie(nazwa)
            ID_NUM - identyfikator polecenia (w odpowiedzi będzie taki sam co przysłany, pozwala na identyfikację 
            czego dotyczy odpowiedź, każde następne o 1 większe) >=1
            PORT - dla pakietów UDP zawiera odpowiadający port TCP, a dla połączeń TCP odpowiadający port UDP
            DATA_LENGTH - dl przesylanych danych
            LAST_PART - czy to ostatni fragment danych, jeżeli nie to można spodziewać się następnych komunikatów związanych z tym poleceniem (wówczas ID_NUM nie będzie zmieniane)
              0 - to nie jest ostatni part
              >0 - to jest ostatni part
              w TCP zawsze >0
            DATA - ewentualne dane
            PART_NUM - numer części (offset) >=0, potrzebny przy wysyłaniu większych komunikatów przez UDP, w TCP nieużywany
            w praktyce raczej nie będziemy tego używali

            wartości liczbowe(w nagłówku) przesyłane w BigEndian (network endian)

            obsługiwane komunikaty:
            GET, DISCONNECT, OK

            obsługiwane odpowiedzi:
            OK, NOTCONNECTED, UNSUPPORTED, BUSY, DATA

            commands e.g.
            klient -> serwer
            3100TGET  - rządanie wysłania bloku danych   (port k+1)
            10200TDISCONNECT - zainicjowanie rozłączenia  (port k+1)

            responses e.g.
            serwer->client
            2100TOK (port k+1) - potwierdzenie odbioru komunikatu, i to że zostaje przetworzone
            12100TNOTCONNECTED (port k+1) - informacja zwrotna, że klient nie jest połączony na porcie k
            4100TBUSY (port k+1) - informacja zwrotna, że serwer aktualnie przetwarza inne żądanie
            4250FDATAVALUE (port k) - komunikat z zawartością bloku danych (lub jego fragmentu, tak jak w tym przypadku)
            4261TDATAVALUE1 (port k) - komunikat z zawartością ostatniej części bloku danych (flaga LAST_PART)

            scenariusze klient->serwer
            I.
            1. klient wysyła GET (port k+1)
            2. serwer odsyła OK, NOTCONNECTED lub BUSY w zależności od sytuacji (port k+1)
            3. jeżeli serwer wysłał OK to przesyła komunikat DATA (port k)

            II.
            1. klient wysyła DISCONNECT (port k+1)
            2. serwer odsyła OK, NOTCONNECTED lub BUSY w zależności od sytuacji (port k+1)
            3. jeżeli serwer wysłał OK, to klient odsyła OK (k+1) i zamyka połączenie (k)
            4. serwer odbiera OK i zamyka połączenie po swojej stronie (k)

            III.
            1. klient wysyła TRALALALA(nie wspierane polecenie) (port k+1)
            2. serwer odsyła NOTCONNECTED lub UNSUPPORTED w zależności od sytuacji (k+1)

            */

            //zmienne na pobrane polecenie i dane
            
            byte[] recv; // tablica na przychodze dane
            byte[] dane; // tablica do ktorej beda serializowane dane
            NetworkStream ns = client.GetStream();  //obiekt strumienia danych 
            while (command == "BUSY" || command == "UNSUPPORTED")
            {
                Thread.Sleep(500);
                ++id_i;
                header_s.id_num = id_i;
                header_s.command_length = 3;
                header_s.port = ((IPEndPoint)client.Client.LocalEndPoint).Port; // port przy wysylaniu UDP jest ustawiony na domyslny lokalny port TCP
                header_s.data_length = 0;
                header_s.part_num = 0;
                header_s.last_part = 1;


                dane = MessageHelper.serialize(ref header_s, "GET", ""); // serializacja danych, dane naglowka w postaci bitow
                // dane oraz polecenie w postaci zakodowanych znakow ASCII
                clientUDP.Send(dane, dane.Length, host);        // wysylanie danych
                Console.WriteLine("3100TGET zostal wyslany* \n");

                //zapisanie odebranych danych to tablicy bajtów:
                recv = clientUDP.Receive(ref host);


                CommandHeader.getHeader(recv, ref header_s); // pobranie danych naglowkowych z tablicy bajtow 
                MessageHelper.unserialize(recv, ref header_s, ref command, ref data); // deserializacja komendy i danych

                //wypisanie danych odpowiedzi na ekran 
                Console.WriteLine("ID_NUM: " + header_s.id_num);
                Console.WriteLine("COMMAND_LENGTH: " + header_s.command_length);
                Console.WriteLine("PORT: " + header_s.port);
                Console.WriteLine("DATA LENGTH: " + header_s.data_length);
                Console.WriteLine("PORT NUM: " + header_s.part_num);
                Console.WriteLine("PORT LAST PART: " + header_s.last_part);
                Console.WriteLine("CMD: " + command);
                Console.WriteLine("DATA SENT: " + data + " \n");
                
            }
            if (command == "NOTCONNECTED")
            {
                while (true)
                {
                    Console.WriteLine("Zrestartuj program, sprawdz polaczenie sieciowe lub poprawnosc danych serwera!");
                    Console.ReadKey();
                }
            }
            else if (command == "OK")
            {
                //Pobranie referencji do obiektu strumienia 
                ns = client.GetStream();


                int totalBytesReaded = 0; // wszystkie odczytane bajty
                int readed = 0; // aktualnie odczytywane bajty
                int bytesReaded = 0; //odczytane bajty w pewnych zakresach (naglowek, cz. danych, komenda etc.)
                byte[] bytes = new byte[CommandHeader.HEADER_SIZE]; // o wielkosci naglowka 
                while (bytesReaded < CommandHeader.HEADER_SIZE)  //odczytywanie czesci naglowkowej:
                {
                    readed = ns.Read(bytes, bytesReaded, CommandHeader.HEADER_SIZE - bytesReaded);
                    totalBytesReaded += readed;
                    bytesReaded += readed;
                }
                CommandHeader header = new CommandHeader();
                //odczytywanie komendy
                if (CommandHeader.getHeader(bytes, ref header))
                {
                    bytes = new byte[header.command_length];
                    bytesReaded = 0;
                    while (bytesReaded < header.command_length)
                    {
                        readed = ns.Read(bytes, bytesReaded, header.command_length - bytesReaded);
                        totalBytesReaded += readed;
                        bytesReaded += readed;
                    }
                    string recv_command = Encoding.ASCII.GetString(bytes); //komenda dekodowana do postaci Stringa

                    //odczytywanie danych:
                    bytes = new byte[header.data_length];
                    bytesReaded = 0;
                    while (bytesReaded < header.data_length)
                    {
                        readed = ns.Read(bytes, bytesReaded, header.data_length - bytesReaded);
                        totalBytesReaded += readed;
                        bytesReaded += readed;
                    }
                    string recv_data = Encoding.ASCII.GetString(bytes); //dane dekodowane do postaci Stringa

                    Console.WriteLine("ID_NUM: " + header.id_num);
                    Console.WriteLine("COMMAND_LENGTH: " + header.command_length);
                    Console.WriteLine("PORT: " + header.port);
                    Console.WriteLine("DATA LENGTH: " + header.data_length);
                    Console.WriteLine("PORT NUM: " + header.part_num);
                    Console.WriteLine("PORT LAST PART: " + header.last_part);
                    Console.WriteLine("CMD: " + recv_command);
                    Console.WriteLine("DATA SENT: " + recv_data + " \n");
                }
            }

            command = "BUSY";
            while (command == "BUSY")
            {
                Thread.Sleep(500);
                //przygotowanie danych do wyslania polecenia DISCONNECT
                id_i++;
                header_s.id_num = id_i;
                header_s.command_length = 10;
                header_s.port = ((IPEndPoint)client.Client.LocalEndPoint).Port;
                header_s.data_length = 0;
                header_s.part_num = 0;
                header_s.last_part = 1;


                // 10200TDISCONNECT
                dane = MessageHelper.serialize(ref header_s, "DISCONNECT", ""); // serializacja datagramu
                clientUDP.Send(dane, dane.Length, host); // wyslanie datagramu 
                Console.WriteLine("10200TDISCONNECT zostal wyslany* \n"); //potwierdzenie wyslania w konsoli
                recv = clientUDP.Receive(ref host); // pobieramy odpowiedz zwrotną

                //zerujemy zmienne
                command = "";
                data = "";
                CommandHeader.getHeader(recv, ref header_s); //pobieranie danych naglowkowych
                MessageHelper.unserialize(recv, ref header_s, ref command, ref data); //deserializacja danych

                //wypisanie odpowiedzi
                Console.WriteLine("ID_NUM: " + header_s.id_num);
                Console.WriteLine("COMMAND_LENGTH: " + header_s.command_length);
                Console.WriteLine("PORT: " + header_s.port);
                Console.WriteLine("DATA LENGTH: " + header_s.data_length);
                Console.WriteLine("PORT NUM: " + header_s.part_num);
                Console.WriteLine("PORT LAST PART: " + header_s.last_part);
                Console.WriteLine("CMD: " + command);
                Console.WriteLine("DATA SENT: " + data + " \n");
            }
            if (command == "NOTCONNECTED")
            {
                while (true)
                {
                    Console.WriteLine("Zrestartuj program, sprawdz polaczenie sieciowe lub poprawnosc danych serwera!");
                    Console.ReadKey();
                }
            }
            else if (command == "OK")
            // jezeli odpowiedz to OK to odsylamy rowniez OK aby zakonczyc polaczenie
            {
               // id_i++;
                header_s.id_num = id_i;
                header_s.command_length = 2;
                header_s.port = ((IPEndPoint)client.Client.LocalEndPoint).Port;
                header_s.data_length = 0;
                header_s.part_num = 0;
                header_s.last_part = 1;
                dane = MessageHelper.serialize(ref header_s, "OK", "");
                clientUDP.Send(dane, dane.Length, host);
                Console.WriteLine("2100TOK zostal wyslany* \n");


                //zamkniecie strumieni
                ns.Close();
                client.Close();
                clientUDP.Close();
                Console.WriteLine("Polaczenie zostalo przerwane, konczenie programu");
                Console.ReadKey();
            }
            
        }
    }
}
