﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace YapperChat.Controls
{
    public partial class DragImage : UserControl
    {
        public DragImage()
        {
            InitializeComponent();
        }

        public Image Image
        {
            get
            {
                return dragImage;
            }
        }
    }
}
