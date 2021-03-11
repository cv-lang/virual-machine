﻿using Cvl.VirtualMachine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cvl.VirtualMachine.Instructions.Exceptions
{
    public class Throw : InstructionBase
    {
        public override void Wykonaj()
        {
            HardwareContext.Status = VirtualMachineState.Exception;
            var rzuconyWyjatek = PopObject();
            MethodContext.WirtualnaMaszyna.EventThrowException(rzuconyWyjatek as Exception);

            ObslugaRzuconegoWyjatku(MethodContext.WirtualnaMaszyna, rzuconyWyjatek);
        }

        public static void ObslugaRzuconegoWyjatku(VirtualMachine wirtualnaMaszyna, object rzuconyWyjatek)
        {
            wirtualnaMaszyna.EventHandleException("Przechodzenie przez stos po metodach obsługi wyjątu");
            var aktywnaMetod = wirtualnaMaszyna.HardwareContext.AktualnaMetoda;
            while (true)
            {
                
                var czyObslugujeWyjatek = aktywnaMetod.CzyObslugujeWyjatki();
                wirtualnaMaszyna.EventHandleException($"Metoda {aktywnaMetod.NazwaMetody}, czy obsługuje wyjątek {czyObslugujeWyjatek}");
                if (czyObslugujeWyjatek)
                {

                    var bloki = aktywnaMetod.PobierzBlokiObslugiWyjatkow();
                    var blok = bloki.FirstOrDefault();
                    if (blok != null)
                    {
                        wirtualnaMaszyna.EventHandleException($"Metoda {aktywnaMetod.NazwaMetody} ... blok obsługi");

                        //obsługujemy pierwszys blok
                        wirtualnaMaszyna.HardwareContext.AktualnaMetoda = aktywnaMetod;
                        wirtualnaMaszyna.HardwareContext.AktualnaMetoda.NumerWykonywanejInstrukcji
                            = aktywnaMetod.PobierzNumerInstrukcjiZOffsetem(blok.HandlerOffset);

                        wirtualnaMaszyna.HardwareContext.Stos.PushObject(rzuconyWyjatek);
                        if (blok.CatchType != null)
                        {
                            //wracam do zwykłej obsługi kodu                            
                            wirtualnaMaszyna.HardwareContext.Status = VirtualMachineState.Executing;
                            wirtualnaMaszyna.EventHandleException($"Metoda {aktywnaMetod.NazwaMetody}, ... wracam do zwykłej obsługi kodu");

                        }
                        else
                        {
                            wirtualnaMaszyna.EventHandleException($"Metoda {aktywnaMetod.NazwaMetody}, ... Brak obsługi blogu Catch w instrukcji Throw");

                            throw new Exception("Brak obsługi blogu Catch w instrukcji Throw");
                        }

                        return;
                    }
                }

                aktywnaMetod = wirtualnaMaszyna.HardwareContext.Stos.PobierzNastepnaMetodeZeStosu();
                if (aktywnaMetod == null)
                {
                    //mamy koniec stosu
                    throw new Exception("Koniec stosu w obsłudze wyjątku", rzuconyWyjatek as Exception);
                }
            }
        }
    }
}
