    ; Luke Groesbeck
    ; RCET3373
    ; Timers
    ; https://github.com/groeluke/LAB_7_ADC/tree/main/LAB_7_Part_1_ADC.X
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
    ;          Bank 1
    BANKSEL TRISA
    BSF     TRISA,0     ;Set RA0 as input for pot 
    BCF     TRISA,1     ;Set RA1 as output for diagnostic bit
    CLRF    TRISB       ;Set PortB as outputs
    CLRF    TRISC       ;Set PortC as outputs
    BSF     ADCON1,7    ;Right justified
    BCF     ADCON1,4    ;Voltage Reference bit for VDD
    BCF     ADCON1,5    ;Voltage Reference bit for VSS
    CLRF    ADRESL      ;Clear low register data
    ;          Bank 3
    BANKSEL ANSEL
    BSF     ANSEL,0     ;Set RA0 as an Analog input
    CLRF    ANSELH
    ;          Bank 0
    BANKSEL ADCON0
    BCF     ADCON0,7    ;Clock Conversion selected bits Fosc/8
    BSF     ADCON0,6    ;TAD = 2us
    CLRF    ADRESH      ;clear high register data
    CLRF    PORTB
    CLRF    PORTC
    BSF     ADCON0,0    ;Enable ADC bit

Main:
    NOP
    NOP
    NOP
    NOP
    NOP                ;delay
    ;       Start conversion
    BANKSEL ADCON0
    BSF     ADCON0,1   ;Set tue A/D conversion status bit high which is automatically
                       ;cleared by hardware when the conversion has completed
    BANKSEL PORTA
    BSF     PORTA,1    ;Set RA1 high for diagnostic

Check:
    BANKSEL ADCON0
    BTFSC   ADCON0,1   ;check to see if its still converting
    GOTO    Check
    BANKSEL PORTA
    BCF     PORTA,1    ;Set RA1 low for diagnostic
    ;       Run the Diagnostic LOW if convertion is complete
    BANKSEL ADRESH
    MOVF    ADRESH,W
    BANKSEL PORTC
    MOVWF   PORTC      ;upper 2 bits to PORTC

    BANKSEL ADRESL
    MOVF    ADRESL,W
    BANKSEL PORTB
    MOVWF   PORTB      ;lower 8 bits to PORTB

    GOTO    Main
    End
    