    ; Luke Groesbeck
    ; RCET3373
    ; UART
    ; https://github.com/groeluke/Lab_8_UART/tree/main/LAB_8_Part_1_UART.X
    ; Linker code = -Wl,-presetVect=0000h,-pisrVect=0004h,-pcode=0008h
    
; PIC16F883 Configuration Bit Settings

; Assembly source line config statements

; CONFIG1
  CONFIG  FOSC = XT             ; Oscillator Selection bits (XT oscillator: Crystal/resonator on RA6/OSC2/CLKOUT and RA7/OSC1/CLKIN)
  CONFIG  WDTE = OFF            ; Watchdog Timer Enable bit (WDT disabled and can be enabled by SWDTEN bit of the WDTCON register)
  CONFIG  PWRTE = OFF           ; Power-up Timer Enable bit (PWRT disabled)
  CONFIG  MCLRE = ON            ; RE3/MCLR pin function select bit (RE3/MCLR pin function is MCLR)
  CONFIG  CP = OFF              ; Code Protection bit (Program memory code protection is disabled)
  CONFIG  CPD = OFF             ; Data Code Protection bit (Data memory code protection is disabled)
  CONFIG  BOREN = OFF           ; Brown Out Reset Selection bits (BOR disabled)
  CONFIG  IESO = OFF            ; Internal External Switchover bit (Internal/External Switchover mode is disabled)
  CONFIG  FCMEN = OFF           ; Fail-Safe Clock Monitor Enabled bit (Fail-Safe Clock Monitor is disabled)
  CONFIG  LVP = OFF             ; Low Voltage Programming Enable bit (RB3 pin has digital I/O, HV on MCLR must be used for programming)

; CONFIG2
  CONFIG  BOR4V = BOR40V        ; Brown-out Reset Selection bit (Brown-out Reset set to 4.0V)
  CONFIG  WRT = OFF             ; Flash Program Memory Self Write Enable bits (Write protection off)

; config statements should precede project file includes.
#include <xc.inc>
  DelayCount  EQU 0x70  ;countdown used inside the delay between UART message
  
; Reset Vector at 0000h. Execution starts here after reset.
PSECT resetVect,class=CODE,delta=2
ResetVector: 
    GOTO Setup

; Interrupt Vector at 0004h. Execution starts here after an interrupt.
PSECT isrVect,class=CODE,delta=2
InterruptVector:
   
; Main code section starts at 0008h. User code goes here.
PSECT code,class=CODE,delta=2
 
Setup: 
    ;          Bank 3
    BANKSEL ANSEL     
    CLRF    ANSEL         ;set all pins to digital which doesn't do anything here.
    CLRF    ANSELH
    CLRF    BAUDCTL       ;clear register for Baud Rate Control Resgister
    ;          Bank 1
    BANKSEL TXSTA 
    MOVLW   0b00100100    ;turn transmitter on, and asynchronus on with high speed,
    MOVWF   TXSTA         ; and 8 bit data
    MOVLW   25
    MOVWF   SPBRG
    BSF     TRISC,6       ;set as asynchronous serial output
    BSF     TRISC,7       ;set as asynchronous serial input
    ;          Bank 0
    BANKSEL RCSTA
    MOVLW   0b10000000    ;enable the serial port
    MOVWF   RCSTA         ;configures RX/DT and TX/CK pins as serial port pins
Main:
    BTFSS   PIR1,4        ;check to see if the transmit register is empty
    GOTO    $-1           ;if not keep waiting
    MakeItRain:
    MOVLW   0x24          ;send the "$"
    MOVWF   TXREG         ;write the transmission
    GOTO    Main          ;Re-run the program
    End
    