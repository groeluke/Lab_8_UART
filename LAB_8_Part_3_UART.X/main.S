    ; Luke Groesbeck
    ; RCET3373
    ; UART
    ; https://github.com/groeluke/Lab_8_UART/tree/main/LAB_8_Part_3_UART.X
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
rx_byte         EQU 0x70        ;latest received byte
rx_state        EQU 0x71        ;0=WAIT_START, 1=GET_COMMAND, 2=GET_LENGTH, 3=GET_CHANNEL
rx_command      EQU 0x72        ;'I' (0x49) or 'A' (0x41)
reply_pending   EQU 0x73        ;0=none, 1=ID, 2=ADC, 3=framing, 4=overrun
adc_state       EQU 0x74        ;0=IDLE, 1=BUSY, 2=READY
adc_high        EQU 0x75        ;ADRESH (top 2 bits of 10-bit result)
adc_low         EQU 0x76        ;ADRESL (bottom 8 bits)
tx_length       EQU 0x77        ;total bytes in the reply packet
tx_count        EQU 0x78        ;how many have already been written to TXREG
tx_data0        EQU 0x79        ;$          (start byte)
tx_data1        EQU 0x7A        ;Command    ('I', 'A', or 'E')
tx_data2        EQU 0x7B        ;Length
tx_data3        EQU 0x7C        ;Data byte 0
tx_data4        EQU 0x7D        ;Data byte 1
tx_data5        EQU 0x7E        ;Data byte 2
Count_5ms       EQU 0x20        ;overrun error stuff
Count_50us      EQU 0x21        ;overrun error inner count
	
;              Required Setup
;EUSART:
;reuse Part 2 operation at 9600 baud, asynchronous 8N1
;ADC:
;reuse working Lab07 AN0 configuration and routines
;use VDD/VSS references, FOSC/8, and a right-justified 10-bit result
;Diagnostic output:
;retain the receive-service timing output from Part 2
;Startup:
;clear parser, reply, ADC, and TX state
;start with the diagnostic output LOW
  
	
; Reset Vector at 0000h. Execution starts here after reset.
PSECT resetVect,class=CODE,delta=2
ResetVector: 
    GOTO Setup

; Interrupt Vector at 0004h. Execution starts here after an interrupt.
PSECT isrVect,class=CODE,delta=2
InterruptVector:
   
; Main code section starts at 0008h. User code goes here.
PSECT code,class=CODE,delta=2
 
; Packets (all binary bytes, '$' = 0x24, 'I' = 0x49, 'A' = 0x41, 'E' = 0x45)
;   ID request     24 49 00              ID reply     24 49 02 33 75
;   ADC request    24 41 01 00           ADC reply    24 41 03 00 HH LL
;   Framing error  24 45 02 01 00        Overrun err  24 45 02 02 00
; Diagnostic pin is RA1 (AN0 uses RA0 for the potentiometer)
 
Setup: 
    ;       Bank 3
    BANKSEL ANSEL
    BSF     ANSEL,0       ;set AN0 as an analog
    BCF     ANSEL,1       ;set AN1 as a digital I/O
    CLRF    BAUDCTL       ;BRG16 = 0 (8-bit baud generator)
    ;       Bank 1
    BANKSEL TXSTA
    MOVLW   0b00100100    ;TXEN=1, BRGH=1, 8-bit, async
    MOVWF   TXSTA
    MOVLW   25            ;9600 baud @ 4 MHz (actual ? 9615)
    MOVWF   SPBRG
    BSF     TRISC,6       ;TX pin
    BSF     TRISC,7       ;RX pin
    BSF     TRISA,0       ;RA0 AN0 input
    BCF     TRISA,1       ;RA1 diagnostic output
    MOVLW   0b10000000    ;ADFM=1 (right-justified), VCFG=00 (VDD/VSS)
    MOVWF   ADCON1
    ;       Bank 0
    BANKSEL RCSTA
    MOVLW   0b10010000    ;enable the serial port and continuous receive
    MOVWF   RCSTA
    MOVLW   0b01000001    ;ADC clock = Fosc/8, Channel = AN0, ADC turned on
    MOVWF   ADCON0
    CLRF    PORTA         ;start with the diagnostic pin low
    CLRF    rx_state      ;parser begins looking for a '$'
    CLRF    rx_command
    CLRF    reply_pending ;no answer waiting to be sent
    CLRF    adc_state     ;ADC is idle
    CLRF    adc_high
    CLRF    adc_low
    CLRF    tx_length     ;nothing in the transmit buffer yet
    CLRF    tx_count
Main:
    BSF     PORTA,1       ;set diagnostic
    BCF     PORTA,1       ;set the dianostic bit low
    CALL    ServiceRx     ;check the receiver
    CALL    ServiceReply  ;hand a received byte over to the send side
    CALL    ServiceTx     ;send it if the transmitter is free
    
; add a 5ms delay to casue over run 
    ;CAll    Delay5ms
    GOTO    Main          ;repeat forever
;ServiceRx: Checks for receive errors first.  If a normal byte arrived, 
;hands it to the parser.  Returns immediately so the main loop can keep running.
ServiceRx:
    BTFSC   RCSTA,1       ;did the receiver overrun?
    GOTO    RxOverrun     ;if high then deal with it
    
    BTFSS   PIR1,5        ;RCIF = 1 means a byte has arrived
    RETURN                ;nothing arrived, nothing to do
    BTFSC   RCSTA,2       ;is the byte framed badly?
    GOTO    RxFraming     ;if high then deal with it
    
    MOVF    RCREG,W       ;read the byte, this also clears RCIF
    MOVWF   rx_byte
    MOVLW   3
    SUBWF   reply_pending,W
    BTFSC   STATUS,0
    RETURN

    CALL    ParseRxByte
    RETURN

; Recover from an overrun: turn continuous receive off and on again,
; empty any leftover bytes, then queue the overrun error packet.
RxOverrun:
    BCF     RCSTA,4       ;CREN off clears the overrun flag
    BSF     RCSTA,4       ;CREN on restarts the receiver
RxFlush:
    BTFSS   PIR1,5
    GOTO    RxOverrunDone
    MOVF    RCREG,W       ;throw away whatever is still in the FIFO
    GOTO    RxFlush
RxOverrunDone:
    MOVLW   4             ;tell ServiceReply we need an overrun packet
    GOTO    QueueError

; Framing error ? read the bad byte (that clears the flag) and queue the
; framing-error packet.
RxFraming:
    MOVF    RCREG,W
    MOVLW   3
QueueError:
    MOVWF   reply_pending
    CLRF    rx_state      ;abandon any partial packet we were building
    CLRF    adc_state     ;also cancel any ADC conversion that was started
    RETURN

;ParseRxByte: Looks at one received byte and advances the little state machine.
; The host may send the bytes of a packet with pauses between them, so we
; remember where we left off.
ParseRxByte:
    MOVF    rx_state,W
    BTFSC   STATUS,2
    GOTO    StateStart
    XORLW   1
    BTFSC   STATUS,2
    GOTO    StateCommand
    MOVF    rx_state,W
    XORLW   2
    BTFSC   STATUS,2
    GOTO    StateLength
    GOTO    StateChannel  ;only possibility left is state 3

;Waiting for the magic start byte
StateStart:
    MOVF    rx_byte,W
    XORLW   0x24          ;is it '$'?
    BTFSS   STATUS,2      ;did the $ come?
    RETURN                ;no ? keep waiting
    MOVLW   1
    MOVWF   rx_state      ;next byte should be the command
    RETURN

;Waiting for the command letter
StateCommand:
    MOVF    rx_byte,W
    XORLW   0x49          ;'I' for device ID?
    BTFSC   STATUS,2      ;is Rx asking for the ID?
    GOTO    SaveCmd
    MOVF    rx_byte,W
    XORLW   0x41          ;'A' for ADC?
    BTFSS   STATUS,2      ;is Rx asking for the ADC reading
    GOTO    ResetParser   ;anything else is ignored
SaveCmd:
    MOVF    rx_byte,W
    MOVWF   rx_command
    MOVLW   2
    MOVWF   rx_state      ;next byte should be the length
    RETURN

;Waiting for the length byte
StateLength:
    MOVF    rx_command,W
    XORLW   0x49          ; was the command 'I'?
    BTFSS   STATUS,2
    GOTO    LengthForA

;   Device-ID request must have length 0
    MOVF    rx_byte,F
    BTFSS   STATUS,2
    GOTO    ResetParser
    CLRF    rx_state      ;request is complete
    MOVF    reply_pending,F ;is another answer already waiting?
    BTFSS   STATUS,2
    RETURN                ;if yes ignore this request
    MOVLW   1
    MOVWF   reply_pending ;queue the ID answer
    RETURN

LengthForA:
;   ADC request must have length 1
    MOVF    rx_byte,W
    XORLW   1
    BTFSS   STATUS,2
    GOTO    ResetParser
    MOVLW   3
    MOVWF   rx_state      ;next (and last) byte is the channel number
    RETURN

;Waiting for the channel number (only used by the ADC request)
StateChannel:
    CLRF    rx_state      ;packet is finished no matter what
    MOVF    rx_byte,F     ;must be channel 0 (AN0)
    BTFSS   STATUS,2
    RETURN                ;any other channel is simply ignored
    MOVF    reply_pending,F
    BTFSS   STATUS,2
    RETURN                ;already have something pending return and ignore the rest
    MOVLW   2
    MOVWF   reply_pending ;queue an ADC answer
    CLRF    adc_state     ;make sure the conversion starts from idle
    RETURN

ResetParser:
    CLRF    rx_state      ;go back to looking for a new '$'
    RETURN

;ServiceReply: When the previous answer has been completely sent and a new 
;one is waiting, this routine builds the complete packet in the six-byte buffer.
ServiceReply:
;   Don?t start a new packet until the old one has finished leaving the UART
    MOVF    tx_length,W
    SUBWF   tx_count,W
    BTFSS   STATUS,0
    RETURN

    MOVF    reply_pending,W
    BTFSC   STATUS,2
    RETURN                  ;nothing waiting

    XORLW   3
    BTFSC   STATUS,2
    GOTO    BuildFraming
    MOVF    reply_pending,W
    XORLW   4
    BTFSC   STATUS,2
    GOTO    BuildOverrun
    MOVF    reply_pending,W
    XORLW   1
    BTFSC   STATUS,2
    GOTO    BuildId

;   Must be an ADC request and keep working on the conversion until it finishes
    CALL    ServiceAdc
    MOVF    adc_state,W
    XORLW   2               ;is the result ready?
    BTFSS   STATUS,2
    RETURN                  ;not yet ? try again next time around the main loop
    GOTO    BuildAdc

;Device ID answer:  $ I 2 0x33 0x75
BuildId:
    MOVLW   0x24
    MOVWF   tx_data0         ;start byte
    MOVLW   0x49
    MOVWF   tx_data0+1       ;command 'I'
    MOVLW   0x02
    MOVWF   tx_data0+2       ;two data bytes follow
    MOVLW   0x33
    MOVWF   tx_data0+3
    MOVLW   0x75
    MOVWF   tx_data0+4
    MOVLW   5
    GOTO    FinishBuild

;ADC answer:  $ A 3 0x00 HH LL
BuildAdc:
    MOVLW   0x24
    MOVWF   tx_data0
    MOVLW   0x41
    MOVWF   tx_data0+1
    MOVLW   0x03
    MOVWF   tx_data0+2       ;three data bytes
    CLRF    tx_data0+3       ;channel 0
    MOVF    adc_high,W
    MOVWF   tx_data0+4
    MOVF    adc_low,W
    MOVWF   tx_data0+5
    CLRF    adc_state       ;ready for the next conversion request
    MOVLW   6
    GOTO    FinishBuild

;Framing-error answer
BuildFraming:
    MOVLW   0x01            ;error code 1
    GOTO    BuildError

;Overrun-error answer
BuildOverrun:
    MOVLW   0x02            ;error code 2

BuildError:
    MOVWF   tx_data0+3       ;the error code goes in the third data byte
    MOVLW   0x24
    MOVWF   tx_data0
    MOVLW   0x45
    MOVWF   tx_data0+1       ;command 'E'
    MOVLW   0x02
    MOVWF   tx_data0+2       ;two data bytes
    CLRF    tx_data0+4       ;detail byte is always zero
    MOVLW   5

FinishBuild:
    MOVWF   tx_length       ;remember how long the packet is
    CLRF    tx_count        ;start sending from the first byte
    CLRF    reply_pending   ;this answer is now in the buffer, so clear the flag
    RETURN

;ServiceAdc: Starts a conversion on one pass through the main 
;loop and collects the result on a later pass.  Never sits and waits.
ServiceAdc:
    MOVF    adc_state,F
    BTFSC   STATUS,2
    GOTO    AdcStart        ;if idle kick off a conversion

    MOVF    adc_state,W
    XORLW   1
    BTFSS   STATUS,2
    RETURN                  ;is it already finished then nothing more to do

;   Conversion is in progress, has it finished yet?
    BTFSC   ADCON0,1        ;GO/DONE bit still set
    RETURN                  ;yes, still busy

;   Is the conversion finished, then save both result bytes
    MOVF    ADRESH,W
    MOVWF   adc_high
    BANKSEL ADRESL
    MOVF    ADRESL,W
    MOVWF   adc_low
    BANKSEL ADCON0          ;make sure we are back in bank 0
    MOVLW   2
    MOVWF   adc_state       ;mark the result as ready
    RETURN

AdcStart:
    BSF     ADCON0,1        ;set the GO/DONE bit to begin the conversion
    MOVLW   1
    MOVWF   adc_state       ;remember that we are now busy
    RETURN

;ServiceTx: If there are still bytes left in the reply packet and 
;the transmitter is empty, send the next one.  Uses the FSR register 
;as a pointer so we can index into the six-byte buffer.
ServiceTx:
    MOVF    tx_length,W
    SUBWF   tx_count,W
    BTFSC   STATUS,0
    RETURN                  ;everything has already been sent

    BTFSS   PIR1,4          ;is the transmit register empty?
    RETURN                  ;if no try again later

;   Point FSR at the next byte that needs to go out
    MOVLW   tx_data0
    ADDWF   tx_count,W      ;W now holds the address of tx_data[tx_count]
    MOVWF   FSR
    MOVF    INDF,W          ;read the byte that FSR is pointing at
    MOVWF   TXREG           ;writing to TXREG starts the transmission
    INCF    tx_count,F      ;one more byte has been queued
    RETURN
    
;Delay 5ms is only here to cause the overrun error,if you want to see the
;over run error remove the ";" in main for the call 5ms delay
Delay5ms:
    MOVLW   0x5E
    MOVWF   Count_5ms
    
Delay5ms_Loop:
    Call    Delay50us_Loop
    DECFSZ  Count_5ms
    GOTO    Delay5ms_Loop
    
    MOVLW   0x04
    MOVWF   Count_5ms

    DECFSZ  Count_5ms
    GOTO    $-1
    RETURN
Delay50us_Loop:
    MOVLW   0x0F            ;1 Tcy move W=15 to count
    MOVWF   Count_50us      ;1 Tcy make count equal 15
                            ;Delay loop is equals 44 Tcy when count is 15         
    DECFSZ  Count_50us,1    ;1 Tcy if not zero, 2 Tcy if zero
    GOTO    $-1             ;2 Tcy delay runs 15 times before skiping
    RETURN

    END