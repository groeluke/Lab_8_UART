    ; Luke Groesbeck
    ; RCET3373
    ; UART
    ; https://github.com/groeluke/Lab_8_UART/tree/main/LAB_8_Part_2_UART.X
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
   rx_byte     EQU 0x70   ;most recently received byte
   tx_byte     EQU 0x71   ;byte waiting to be sent back
   rx_ready    EQU 0x72   ;bit0: 1 = rx_byte has a byte not handled yet
   tx_pending  EQU 0x73   ;bit0: 1 = tx_byte is waiting to be sent
  
  ;Required Setup
  ;EUSART receiver:
  ;use the Part 1 9600 baud, 8N1 configuration
  ;enable continuous reception of 8-bit data
  ;Diagnostic output:
  ;configure an unused pin as a digital output
  ;Startup:
  ;clear rx_ready and tx_pending
  ;start with the diagnostic output LOW
  
  ;Program Structure
;main:
;    call ServiceRx
;    call ProcessEcho
;    call ServiceTx
;    repeate
;ServiceRx:
;    set the diagnostic output HIGH, then LOW
;    if rx_ready is set, return
;    if no byte has arrived, return
;    read the received byte into rx_byte
;    set rx_ready
;ProcessEcho:
;    if rx_ready is clear, return
;    if tx_pending is set, return
;    copy rx_byte into tx_byte
;    clear rx_ready
;    set tx_pending
;ServiceTx:
;    if tx_pending is clear, return
;    if the transmit buffer is not available, return
;    transmit tx_byte
;    clear tx_pending

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
    ;       Bank 3
    BANKSEL ANSEL     
    CLRF    ANSEL         ;set all pins digital, RA0 needs this as an output
    CLRF    ANSELH
    CLRF    BAUDCTL       ;set the 8-bit baud rate generator
    ;       Bank 1
    BANKSEL TXSTA 
    MOVLW   0b00100100    ;transmitter on, asynchronous, high speed,
    MOVWF   TXSTA         ;and 8 bit data
    MOVLW   25
    MOVWF   SPBRG         ;9600 baud divisor, 9615 actual at 4 MHz
    BCF     TRISC,6       ;bit set as the data sheet says for the TX pin
    BSF     TRISC,7       ;bit set as the data sheet says for the RX pin
    BCF     TRISA,0       ;RA0 is an output, the diagnostic pin
    ;       Bank 0
    BANKSEL RCSTA
    MOVLW   0b10010000    ;SPEN = 1 enables the serial port,
    MOVWF   RCSTA         ;CREN = 1 turns on continuous receive
    CLRF    PORTA         ;diagnostic pin starts LOW
    CLRF    rx_ready      ;no byte received yet
    CLRF    tx_pending    ;nothing waiting to send
Main:
    CALL    ServiceRx     ;check the receiver
    CALL    ProcessEcho   ;hand a received byte over to the send side
    CALL    ServiceTx     ;send it if the transmitter is free
    GOTO    Main          ;repeat forever
; ServiceRx: the pulse on RA0 marks every receive check. Then, only
; if the last byte was handled and a new one arrived, read it.
ServiceRx:
    BANKSEL PORTA
    BSF     PORTA,0       ;diagnostic pulse HIGH
    BCF     PORTA,0       ;and LOW on the very next instruction
    BTFSC   rx_ready,0    ;is the last byte still waiting to be handled?
    RETURN                ;yes, don't overwrite it
    BTFSS   PIR1,5        ;RCIF = 1 means a byte has arrived
    RETURN                ;nothing arrived, nothing to do
    MOVF    RCREG,W       ;read the byte, this also clears RCIF
    MOVWF   rx_byte
    BSF     rx_ready,0    ;tell ProcessEcho there is a byte
    BTFSS   RCSTA,1       ;OERR = 1 means the receiver overran
    RETURN                ;no overrun, done
    BCF     RCSTA,4       ;overrun: turn CREN off
    BSF     RCSTA,4       ;and back on, which clears OERR
    RETURN
; ProcessEcho: if a byte is waiting and the last echo is gone,
; copy the byte over unchanged.
ProcessEcho:
    BTFSS   rx_ready,0    ;is there a received byte?
    RETURN                ;no
    BTFSC   tx_pending,0  ;is the last echo still waiting to go out?
    RETURN                ;yes, wait for it
    MOVF    rx_byte,W
    MOVWF   tx_byte       ;copy the byte, unchanged
    BCF     rx_ready,0    ;received byte is handled
    BSF     tx_pending,0  ;and now it waits to be sent
    RETURN
; ServiceTx: if a byte is waiting and TXREG is empty, send it.
ServiceTx:
    BTFSS   tx_pending,0  ;is anything waiting to be sent?
    RETURN                ;no
    BTFSS   PIR1,4        ;TXIF = 1 means TXREG is empty
    RETURN                ;transmitter busy, try again next pass
    MOVF    tx_byte,W
    MOVWF   TXREG         ;writing here starts the transmission
    BCF     tx_pending,0  ;it is on its way
    RETURN
    End
    