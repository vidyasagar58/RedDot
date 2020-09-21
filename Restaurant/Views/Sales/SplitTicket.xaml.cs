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
    /// Interaction logic for SplitTicket.xaml
    /// </summary>
    public partial class SplitTicket : Window
    {
        SplitTicketVM splitvm;
        public SplitTicket(SecurityModel security,Ticket currentticket)
        {
            InitializeComponent();
            splitvm = new SplitTicketVM(this,security,currentticket);
            this.DataContext = splitvm;
        }

   
    }
}
