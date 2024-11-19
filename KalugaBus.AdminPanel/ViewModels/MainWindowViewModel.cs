using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KalugaBus.AdminPanel.Models;

namespace KalugaBus.AdminPanel.ViewModels;

public class MainWindowViewModel : BaseViewModel
{
    public int PointSelectedRouteIndex { get; set; }
}