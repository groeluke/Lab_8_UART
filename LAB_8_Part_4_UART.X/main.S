    ; Luke Groesbeck
    ; RCET3373
    ; UART
    ; https://github.com/groeluke/Lab_8_UART/tree/main/LAB_8_Part_4_UART.X
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
W_TEMP          EQU 0x70        ;W saved during the ISR
STATUS_TEMP     EQU 0x71        ;STATUS saved during the ISR
servo_busy      EQU 0x72        ;bit0 = 1 while the servo pulse (RC2) is HIGH
rx_byte         EQU 0x73        ;latest received byte
rx_state        EQU 0x74        ;0 WAIT_START, 1 GET_COMMAND, 2 GET_LENGTH, 3 GET_CHANNEL, 4 GET_MATCH_H, 5 GET_MATCH_L
rx_command      EQU 0x75        ;'I' 0x49, 'A' 0x41, or 'S' 0x53
reply_pending   EQU 0x76        ;0 none, 1 ID, 2 ADC, 3 servo error, 4 framing, 5 overrun
adc_state       EQU 0x77        ;0 IDLE, 1 BUSY, 2 READY
adc_high        EQU 0x78        ;top 2 bits of the 10-bit result
adc_low         EQU 0x79        ;bottom 8 bits of the 10-bit result
tx_length       EQU 0x7A        ;bytes in the reply packet
tx_count        EQU 0x7B        ;bytes already sent
rx_match_h      EQU 0x7C        ;high byte of an S command, held until the low byte arrives
servo_next_h    EQU 0x7D        ;saved compare value for the NEXT frame, high byte
servo_next_l    EQU 0x7E        ;saved compare value for the NEXT frame, low byte

;bank 0 RAM, the reply buffer that only main-loop code touches it
tx_data0        EQU 0x20        ;$
tx_data1        EQU 0x21        ;command
tx_data2        EQU 0x22        ;length
tx_data3        EQU 0x23        ;data 0
tx_data4        EQU 0x24        ;data 1
tx_data5        EQU 0x25        ;data 2
	
;              Required Setup
; Packets ('$' 0x24, 'I' 0x49, 'A' 0x41, 'S' 0x53, 'E' 0x45)
; ID request      24 49 00              reply  24 49 02 33 75
; ADC request     24 41 01 00           reply  24 41 03 00 HH LL
; Servo command   24 53 02 MH ML        no reply when valid
; Servo error     24 45 02 03 01        (compare value outside B3D4 to BBA4)
; Framing error   24 45 02 01 00        Overrun error  24 45 02 02 00
; Servo compare = 0xB1E0 + pulse width in us:  500 = B3D4, 1500 = B7BC, 2500 = BBA4
; RA0 = LM34 on AN0, RA1 = receive-service diagnostic, RC2 = servo output
  
	
; Reset Vector at 0000h. Execution starts here after reset.
PSECT resetVect,class=CODE,delta=2
ResetVector: 
    GOTO Setup

; Interrupt Vector at 0004h. Execution starts here after an interrupt.
PSECT isrVect,class=CODE,delta=2
InterruptVector:
    GOTO    IsrHandler
   
; Main code section starts at 0008h. User code goes here.
PSECT code,class=CODE,delta=2
 
Setup: 
    ;       Bank 3
    BANKSEL ANSEL
    BSF     ANSEL,0       ;set AN0 as an analog
    BCF     ANSEL,1       ;set AN1 as a digital I/O
    CLRF    ANSELH
    CLRF    BAUDCTL       ;BRG16 = 0 (8-bit baud generator)
    ;       Bank 1
    BANKSEL TXSTA
    MOVLW   0b00100100    ;transmitter on, high-speed baud
    MOVWF   TXSTA
    MOVLW   25            ;9600 baud @ 4 MHz
    MOVWF   SPBRG

    BSF     TRISC,6       ;TX pin (RC6) as input (peripheral controls it)
    BSF     TRISC,7       ;RX pin (RC7) as input
    BCF     TRISC,2       ;RC2 = servo output
    BSF     TRISA,0       ;RA0 = LM34 input
    BCF     TRISA,1       ;RA1 = diagnostic pulse output

    MOVLW   0b10000000
    MOVWF   ADCON1      ;Right justified amd VDD/VSS references
    
    MOVLW   0b00000101    ;enable Timer1 and CCP1 interrupts
    MOVWF   PIE1
    ;       Bank 0
    BANKSEL RCSTA
    MOVLW   0b10010000    ;serial port on + continuous receive
    MOVWF   RCSTA
    MOVLW   0b01000001    ;ADC: Fosc/8, channel AN0, ADC on
    MOVWF   ADCON0

    CLRF    PORTA
    CLRF    PORTC
    CLRF    servo_busy
    CLRF    rx_state
    CLRF    reply_pending
    CLRF    adc_state
    CLRF    tx_length
    CLRF    tx_count

    ;Start the servo at 1500 µs (center position)
    ;0xB1E0 + 1500 = 0xB7BC
    MOVLW   0xB7
    MOVWF   servo_next_h
    MOVWF   CCPR1H
    MOVLW   0xBC
    MOVWF   servo_next_l
    MOVWF   CCPR1L

    MOVLW   0b00001010    ;CCP1 in compare mode, interrupt on match
    MOVWF   CCP1CON

    ; Timer1 starts at 0xB1E0 so it overflows every 20 000 µs (20 ms)
    MOVLW   0xB1
    MOVWF   TMR1H
    MOVLW   0xE0
    MOVWF   TMR1L
    BCF     PIR1,0        ;clear Timer1 flag
    BCF     PIR1,2        ;clear CCP1 flag
    MOVLW   0b00000001    ;Timer1 on, 1:1 prescale, internal clock
    MOVWF   T1CON

    BSF     INTCON,6      ;allow peripheral interrupts
    BSF     INTCON,7      ;global interrupts on
    
;Servo pulse ats the highest priority, 
;While RC2 is high we do nothing in the foreground.
Main:
    BTFSC   servo_busy,0  ;is the servo pulse currently HIGH?
    GOTO    Main          ;if yes wait until it finishes

;Diagnostic pulse on RA1 that shows how often we get to service the UART
    BSF     PORTA,1
    BCF     PORTA,1

    CALL    ServiceRx     ;look for received bytes / errors
    BTFSC   servo_busy,0
    GOTO    Main

    CALL    ServiceReply   ;build a reply packet if one is waiting
    BTFSC   servo_busy,0
    GOTO    Main

    CALL    ServiceTx      ;Send the next byte of a reply if ready
    GOTO    Main
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
    MOVLW   5             ;tell ServiceReply we need an overrun packet
    GOTO    QueueError

; Framing error then read the bad byte (that clears the flag) and queue the
; framing-error packet.
RxFraming:
    MOVF    RCREG,W
    MOVLW   4
QueueError:
    MOVWF   reply_pending
    CLRF    rx_state      ;abandon any partial packet we were building
    CLRF    adc_state     ;also cancel any ADC conversion that was started
    RETURN

;ParseRxByte: Looks at one received byte and advances the little state machine.
;The host may send the bytes of a packet with pauses between them, so we
;remember where we left off.
ParseRxByte:
    MOVF    rx_state,W
    XORLW   0
    BTFSC   STATUS,2
    GOTO    StateStart

    MOVF    rx_state,W
    XORLW   1
    BTFSC   STATUS,2
    GOTO    StateCommand

    MOVF    rx_state,W
    XORLW   2
    BTFSC   STATUS,2
    GOTO    StateLength

    MOVF    rx_state,W
    XORLW   3
    BTFSC   STATUS,2
    GOTO    StateChannel

    MOVF    rx_state,W
    XORLW   4
    BTFSC   STATUS,2
    GOTO    StateMatchH

    MOVF    rx_state,W
    XORLW   5
    BTFSC   STATUS,2
    GOTO    StateMatchL

    ; Unexpected state: reset parser
    CLRF    rx_state
    RETURN

;Waiting for the magice start byte
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
    MOVWF   rx_command          ; save it
    XORLW   0x49                ; 'I'?
    BTFSC   STATUS,2
    GOTO    CmdOk
    MOVF    rx_command,W
    XORLW   0x41                ; 'A'?
    BTFSC   STATUS,2
    GOTO    CmdOk
    MOVF    rx_command,W
    XORLW   0x53                ; 'S'?
    BTFSS   STATUS,2
    GOTO    ResetParser         ; unknown command
CmdOk:
    MOVLW   2
    MOVWF   rx_state
    RETURN

;Waiting for the length byte
StateLength:
    MOVF    rx_command,W
    XORLW   0x49                ; was it ID?
    BTFSC   STATUS,2
    GOTO    LenID
    MOVF    rx_command,W
    XORLW   0x53                ; was it Servo?
    BTFSC   STATUS,2
    GOTO    LenServo

    ; Must be ADC ? length must be 1
    MOVF    rx_byte,W
    XORLW   1
    BTFSS   STATUS,2
    GOTO    ResetParser
    MOVLW   3
    MOVWF   rx_state            ; next byte is the channel
    RETURN

LenID:
    MOVF    rx_byte,F           ; length must be 0
    BTFSS   STATUS,2
    GOTO    ResetParser
    CLRF    rx_state
    MOVF    reply_pending,F
    BTFSS   STATUS,2
    RETURN                      ; already have a reply pending
    MOVLW   1
    MOVWF   reply_pending       ; queue ID reply
    RETURN

LenServo:
    MOVF    rx_byte,W
    XORLW   2                   ; length must be 2
    BTFSS   STATUS,2
    GOTO    ResetParser
    MOVLW   4
    MOVWF   rx_state            ; next byte is match high
    RETURN

;Waiting for the channel number which only used by the ADC request
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

; Servo ? high byte of the compare value
StateMatchH:
    MOVF    rx_byte,W
    MOVWF   rx_match_h
    MOVLW   5
    MOVWF   rx_state
    RETURN

;Servo ? low byte arrived, now validate the 16-bit value
StateMatchL:
    CLRF    rx_state

    ; Check that the value is between 0xB3D4 and 0xBBA4 (500?2500 µs)
    ; First check minimum
    MOVF    rx_match_h,W
    SUBLW   0xB3                ; 0xB3 - high
    BTFSS   STATUS,0            ; high > 0xB3?
    GOTO    CheckMax
    BTFSS   STATUS,2            ; high == 0xB3?
    GOTO    ServoBad            ; high < 0xB3 too small
    ; high == 0xB3 and low must be >= 0xD4
    MOVF    rx_byte,W
    SUBLW   0xD3
    BTFSC   STATUS,0            ; low <= 0xD3?
    GOTO    ServoBad

CheckMax:
    MOVF    rx_match_h,W
    SUBLW   0xBB                ; 0xBB - high
    BTFSS   STATUS,0            ; high > 0xBB?
    GOTO    ServoBad
    BTFSS   STATUS,2            ; high == 0xBB?
    GOTO    ServoGood           ; high < 0xBB ? good
    ; high == 0xBB ? low must be <= 0xA4
    MOVF    rx_byte,W
    SUBLW   0xA4
    BTFSS   STATUS,0            ; low > 0xA4?
    GOTO    ServoBad

; Value is valid ? save it for the NEXT frame (with interrupts off)
ServoGood:
    BCF     INTCON,7            ; turn interrupts off
    BTFSC   INTCON,7            ; make sure they are really off
    GOTO    $-2
    MOVF    rx_match_h,W
    MOVWF   servo_next_h
    MOVF    rx_byte,W
    MOVWF   servo_next_l
    BSF     INTCON,7            ; interrupts back on
    RETURN                      ; valid S command has no reply

; Value was outside the legal range
ServoBad:
    MOVF    reply_pending,F
    BTFSS   STATUS,2
    RETURN
    MOVLW   3
    MOVWF   reply_pending       ; queue servo error packet
    RETURN

ResetParser:
    CLRF    rx_state
    RETURN


;ServiceReply: When the previous answer has been completely sent and a new 
;one is waiting, this routine builds the complete packet in the six-byte buffer.
ServiceReply:
;   Don't start a new packet until the old one has finished leaving the UART
    MOVF    tx_length,W
    SUBWF   tx_count,W
    BTFSS   STATUS,0
    RETURN

    MOVF    reply_pending,W
    BTFSC   STATUS,2
    RETURN                  ;nothing waiting

; Decide which packet to build
    XORLW   1
    BTFSC   STATUS,2
    GOTO    BuildId
    MOVF    reply_pending,W
    XORLW   2
    BTFSC   STATUS,2
    GOTO    BuildAdcWait
    MOVF    reply_pending,W
    XORLW   3
    BTFSC   STATUS,2
    GOTO    BuildServoErr
    MOVF    reply_pending,W
    XORLW   4
    BTFSC   STATUS,2
    GOTO    BuildFraming
    GOTO    BuildOverrun        ; must be 5
    
;   Must be an ADC request and keep working on the conversion until it finishes
BuildAdcWait:
    CALL    ServiceAdc
    MOVF    adc_state,W
    XORLW   2
    BTFSS   STATUS,2
    RETURN                      ; conversion not finished yet
    GOTO    BuildAdc

; Device ID reply: 24 49 02 33 75
BuildId:
    MOVLW   0x24
    MOVWF   tx_data0
    MOVLW   0x49
    MOVWF   tx_data1
    MOVLW   0x02
    MOVWF   tx_data2
    MOVLW   0x33
    MOVWF   tx_data3
    MOVLW   0x75
    MOVWF   tx_data4
    MOVLW   5
    GOTO    FinishBuild

; ADC / temperature reply: 24 41 03 00 HH LL
BuildAdc:
    MOVLW   0x24
    MOVWF   tx_data0
    MOVLW   0x41
    MOVWF   tx_data1
    MOVLW   0x03
    MOVWF   tx_data2
    CLRF    tx_data3            ; channel 0
    MOVF    adc_high,W
    MOVWF   tx_data4
    MOVF    adc_low,W
    MOVWF   tx_data5
    CLRF    adc_state
    MOVLW   6
    GOTO    FinishBuild

; Servo range error: 24 45 02 03 01
BuildServoErr:
    MOVLW   0x03
    MOVWF   tx_data3
    MOVLW   0x01
    MOVWF   tx_data4
    GOTO    BuildErrCommon

; Framing error: 24 45 02 01 00
BuildFraming:
    MOVLW   0x01
    MOVWF   tx_data3
    CLRF    tx_data4
    GOTO    BuildErrCommon

; Overrun error: 24 45 02 02 00
BuildOverrun:
    MOVLW   0x02
    MOVWF   tx_data3
    CLRF    tx_data4

BuildErrCommon:
    MOVLW   0x24
    MOVWF   tx_data0
    MOVLW   0x45
    MOVWF   tx_data1
    MOVLW   0x02
    MOVWF   tx_data2
    MOVLW   5

FinishBuild:
    MOVWF   tx_length
    CLRF    tx_count
    CLRF    reply_pending
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
    
;The ISRHandler was pulled from the servo lab and handles everything
IsrHandler:
    MOVWF   W_TEMP              ; save W
    SWAPF   STATUS,W
    MOVWF   STATUS_TEMP         ; save STATUS

    BANKSEL PIR1
    BTFSC   PIR1,0              ; Timer1 overflow?
    CALL    Service_Timer1
    BTFSC   PIR1,2              ; CCP1 match?
    CALL    Service_CCP1

    SWAPF   STATUS_TEMP,W
    MOVWF   STATUS
    SWAPF   W_TEMP,F
    SWAPF   W_TEMP,W
    RETFIE

; New 20 ms frame begins
Service_Timer1:
    BCF     PIR1,0
    MOVLW   0xB1
    MOVWF   TMR1H               ; reload for next 20 ms
    MOVLW   0xE0
    MOVWF   TMR1L

    BSF     PORTC,2             ; start the servo pulse (RC2 HIGH)
    BSF     servo_busy,0

    ; Load the compare value that was saved by a previous S command
    MOVF    servo_next_h,W
    MOVWF   CCPR1H
    MOVF    servo_next_l,W
    MOVWF   CCPR1L
    RETURN

; Compare match ? end of the servo pulse
Service_CCP1:
    BCF     PIR1,2
    BCF     PORTC,2             ; RC2 LOW
    BCF     servo_busy,0
    RETURN
    
    END