# Getting Started

## UHF RFID tag memory

The tag memory is conceptually comprised of the following distinct
memory banks:

* reserved memory (something tag-related that apps don't usually work with)
* *TID*: tag identification, for looking up information about the tag
  (maker, model, etc.); sometimes this includes a serial which is
  guaranteed by the vendor to be unique among the tags produced by the
  vendor
* *EPC*: this is used for storing the *Electronic Product Code*
* *USER*: this is usually blank, and can be used by the apps; although
  there exist a standard for it, all bets are off

All memory banks except TID and reserved are usually writable.

## A word about EPCs

Depending on the vendor, some tags coming fresh off the factory will
have an empty EPC, or EPC that is only partially filled in. The Tag
Data Structure standard by EPCglobal describes the binary format and
the encoding/decoding procedure for the EPC memory bank.

SGTIN (Serialized GTIN) is the most useful scheme for EPCs. It consists
of

* a *GTIN-14* (i.e., a barcode, itself consisting of company prefix,
  assigned by GS1, and the item reference)
* and a *serial number* (this one usually comes from an information system's
  database, and is used for assignign consecutively increasing numbers)

Taken together, these pieces help to identify a particular *instance*
of a *product*.

## Different representations of EPCs

A given SGTIN EPC can be represented differently:

* written on the tag in a compact SGTIN-192 form
* reside in memory of a computer program in a textual SGTIN Identity
  URI form, or SGTIN Tag URI form

*Tag URI*: this contains the full decoded contents of an EPC memory
bank, making roundtrip between the URI and the memory bank possible.

*Identity URI*: this contains only the application-specific data of a
Tag URI (or the EPC memory bank); so it is intended for identification
(roundtrip is only possible if you are okay with reconstructing some
missing information from the identity URI).

Here's a hopefully helpful diagram:

EPC memory bank <=> Tag URI <=> Identity URI

## Serialization

Remember the *serial number* that is present in SGTIN? It can be
obtained in the following ways:

* from a central database
* from the chip's TID memory (via MCS)

Assuming a suitable chip, via MCS an algorithm can extract the serial
number from the TID memory bank, and write it to the tag; no central
database is necessary.
