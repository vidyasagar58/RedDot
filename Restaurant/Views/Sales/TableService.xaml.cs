﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace RedDot
{
    /// <summary>
    /// Interaction logic for Table.xaml
    /// </summary>
    public partial class TableService : Window
    {

        public TableServiceVM tableviewmodel;
        SecurityModel m_security;
       
        public TableService(SecurityModel security)
        {
            InitializeComponent();
            m_security = security;
            tableviewmodel = new TableServiceVM(security,this);
            this.DataContext = tableviewmodel;
        }

    

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

  
    }
}
